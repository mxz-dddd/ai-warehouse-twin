using System.Text.Json;

namespace Sim.Validation;

/// <summary>
/// Validates a customer-provided warehouse scenario document
/// (<c>schema_version = "warehouse-scenario.v0"</c>). The walk accumulates every
/// problem in one pass and reports each with a JSON path so the customer can
/// locate and fix it. Consumes only the scenario JSON; depends on nothing but the
/// base class library.
/// </summary>
public static class ScenarioValidator
{
    public const string ExpectedSchemaVersion = "warehouse-scenario.v0";

    // Lowercase projection of the InventoryUnit.status enum from the shared domain
    // contract (packages/contracts/domain/inventory_unit.schema.json). Scenario
    // inventory uses the lowercase form (e.g. "available"); kept aligned with the
    // contract so a domain-legal status is never wrongly rejected. The exact subset
    // the run-file parser accepts is owned by member 1 and confirmed in review.
    private static readonly string[] AllowedInventoryStatusList =
    {
        "expected", "received", "qc_hold", "available", "allocated",
        "picking", "picked", "consolidating", "staged", "loaded", "shipped",
    };

    /// <summary>Inventory status values this validator accepts (lowercase). Kept in
    /// sync with the <c>status</c> enum in scenario.schema.json.</summary>
    public static IReadOnlyList<string> AllowedInventoryStatuses => AllowedInventoryStatusList;

    private static readonly HashSet<string> AllowedStatusSet =
        new(AllowedInventoryStatusList, StringComparer.Ordinal);

    public static ScenarioValidationResult Validate(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            var where = ex.LineNumber is { } line
                ? $"第 {line + 1} 行第 {(ex.BytePositionInLine ?? 0) + 1} 列附近"
                : "文件中";
            return new ScenarioValidationResult(false, new[]
            {
                new ScenarioValidationError("$", "json.parse_error",
                    $"文件不是合法的 JSON:{where}解析失败,请检查括号、逗号和引号是否成对。"),
            });
        }

        using (doc)
        {
            var ctx = new Ctx();
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                ctx.Add("$", "scenario.not_object", "场景文件的顶层应为一个 JSON 对象(用 { } 包裹)。");
                return ctx.ToResult();
            }

            ValidateRoot(ctx, root);
            return ctx.ToResult();
        }
    }

    private static void ValidateRoot(Ctx ctx, JsonElement root)
    {
        if (!root.TryGetProperty("schema_version", out var sv) || sv.ValueKind == JsonValueKind.Null)
        {
            ctx.Missing("$", "schema_version");
        }
        else if (sv.ValueKind != JsonValueKind.String)
        {
            ctx.TypeError("$.schema_version", "schema_version", JsonValueKind.String, sv);
        }
        else if (sv.GetString() != ExpectedSchemaVersion)
        {
            ctx.Add("$.schema_version", "schema_version.unsupported",
                $"{FieldLabel("schema_version")} 应为 \"{ExpectedSchemaVersion}\",当前为 \"{sv.GetString()}\"。");
        }

        RequireString(ctx, root, "$", "scenario_id");
        RequireInteger(ctx, root, "$", "seed", min: 0);
        OptionalString(ctx, root, "$", "description");

        // A flow counts as "provided" when its key is present (even if null). A
        // present-but-null or non-object flow is then reported as a type error by
        // the Validate* method below. This keeps the accept/reject verdict identical
        // to scenario.schema.json, whose anyOf is satisfied by key presence.
        var hasInbound = root.TryGetProperty("inbound", out var inbound);
        var hasOutbound = root.TryGetProperty("outbound", out var outbound);
        var hasEachPick = root.TryGetProperty("each_pick", out var eachPick);

        if (!hasInbound && !hasOutbound && !hasEachPick)
        {
            ctx.Add("$", "scenario.no_flow",
                "场景至少要包含 inbound(入库)、outbound(出库)、each_pick(拣选)之中的一个流程,否则没有任何作业可以仿真。");
        }

        if (hasInbound)
        {
            ValidateInbound(ctx, inbound);
        }

        if (hasOutbound)
        {
            ValidateOutbound(ctx, outbound);
        }

        if (hasEachPick)
        {
            ValidateEachPick(ctx, eachPick);
        }

        if (root.TryGetProperty("expected", out var expected))
        {
            ValidateExpected(ctx, expected);
        }
    }

    private static void ValidateInbound(Ctx ctx, JsonElement e)
    {
        const string p = "$.inbound";
        if (e.ValueKind != JsonValueKind.Object)
        {
            ctx.NotObject(p, "inbound 入库流程", e);
            return;
        }

        OptionalString(ctx, e, p, "scenario_id");
        OptionalInteger(ctx, e, p, "seed", min: 0);
        RequireInteger(ctx, e, p, "dock_count", min: 1);
        RequireInteger(ctx, e, p, "forklift_count", min: 1);

        if (RequireObject(ctx, e, p, "process", out var proc))
        {
            var pp = p + ".process";
            RequireInteger(ctx, proc, pp, "unload_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "qc_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "putaway_duration_ms", min: 0);
        }

        if (RequireNonEmptyArray(ctx, e, p, "receipts", out var receipts))
        {
            var i = 0;
            foreach (var r in receipts.EnumerateArray())
            {
                var rp = $"{p}.receipts[{i}]";
                if (r.ValueKind != JsonValueKind.Object)
                {
                    ctx.NotObject(rp, "每条入库收货", r);
                }
                else
                {
                    RequireString(ctx, r, rp, "receipt_id");
                    RequireString(ctx, r, rp, "warehouse_id");
                    RequireString(ctx, r, rp, "sku_id");
                    RequireInteger(ctx, r, rp, "quantity", min: 1);
                    RequireString(ctx, r, rp, "staging_location_id");
                    RequireString(ctx, r, rp, "storage_location_id");
                    RequireInteger(ctx, r, rp, "arrives_at_ms", min: 0);
                }

                i++;
            }
        }
    }

    private static void ValidateOutbound(Ctx ctx, JsonElement e)
    {
        const string p = "$.outbound";
        if (e.ValueKind != JsonValueKind.Object)
        {
            ctx.NotObject(p, "outbound 出库流程", e);
            return;
        }

        OptionalString(ctx, e, p, "scenario_id");
        OptionalInteger(ctx, e, p, "seed", min: 0);
        RequireInteger(ctx, e, p, "worker_count", min: 1);
        RequireInteger(ctx, e, p, "dock_count", min: 1);

        if (RequireObject(ctx, e, p, "process", out var proc))
        {
            var pp = p + ".process";
            RequireInteger(ctx, proc, pp, "pick_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "stage_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "dock_travel_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "load_duration_ms", min: 0);
        }

        if (RequireNonEmptyArray(ctx, e, p, "inventory", out var inventory))
        {
            ValidateInventoryList(ctx, inventory, p);
        }

        if (RequireNonEmptyArray(ctx, e, p, "orders", out var orders))
        {
            var i = 0;
            foreach (var o in orders.EnumerateArray())
            {
                var op = $"{p}.orders[{i}]";
                if (o.ValueKind != JsonValueKind.Object)
                {
                    ctx.NotObject(op, "每条出库订单", o);
                }
                else
                {
                    RequireString(ctx, o, op, "order_id");
                    RequireString(ctx, o, op, "warehouse_id");
                    RequireString(ctx, o, op, "sku_id");
                    RequireInteger(ctx, o, op, "quantity", min: 1);
                    RequireString(ctx, o, op, "pick_location_id");
                    RequireString(ctx, o, op, "staging_location_id");
                    RequireString(ctx, o, op, "dock_id");
                    RequireInteger(ctx, o, op, "released_at_ms", min: 0);
                }

                i++;
            }
        }
    }

    private static void ValidateEachPick(Ctx ctx, JsonElement e)
    {
        const string p = "$.each_pick";
        if (e.ValueKind != JsonValueKind.Object)
        {
            ctx.NotObject(p, "each_pick 拣选流程", e);
            return;
        }

        OptionalString(ctx, e, p, "scenario_id");
        OptionalInteger(ctx, e, p, "seed", min: 0);
        RequireInteger(ctx, e, p, "station_count", min: 1);
        RequireInteger(ctx, e, p, "worker_count", min: 1);

        if (RequireObject(ctx, e, p, "process", out var proc))
        {
            var pp = p + ".process";
            RequireInteger(ctx, proc, pp, "tote_bind_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "travel_to_station_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "pick_service_duration_ms", min: 0);
            RequireInteger(ctx, proc, pp, "move_to_staging_duration_ms", min: 0);
        }

        if (RequireNonEmptyArray(ctx, e, p, "inventory", out var inventory))
        {
            ValidateInventoryList(ctx, inventory, p);
        }

        if (RequireNonEmptyArray(ctx, e, p, "orders", out var orders))
        {
            var i = 0;
            foreach (var o in orders.EnumerateArray())
            {
                var op = $"{p}.orders[{i}]";
                if (o.ValueKind != JsonValueKind.Object)
                {
                    ctx.NotObject(op, "每条拣选订单", o);
                }
                else
                {
                    RequireString(ctx, o, op, "order_id");
                    RequireString(ctx, o, op, "warehouse_id");
                    RequireString(ctx, o, op, "sku_id");
                    RequireInteger(ctx, o, op, "quantity", min: 1);
                    RequireString(ctx, o, op, "pick_face_location_id");
                    RequireString(ctx, o, op, "pick_station_id");
                    RequireString(ctx, o, op, "staging_location_id");
                    RequireInteger(ctx, o, op, "released_at_ms", min: 0);
                }

                i++;
            }
        }
    }

    private static void ValidateInventoryList(Ctx ctx, JsonElement inventory, string flowPath)
    {
        var i = 0;
        foreach (var inv in inventory.EnumerateArray())
        {
            var ip = $"{flowPath}.inventory[{i}]";
            if (inv.ValueKind != JsonValueKind.Object)
            {
                ctx.NotObject(ip, "每条库存", inv);
            }
            else
            {
                RequireString(ctx, inv, ip, "inventory_id");
                RequireString(ctx, inv, ip, "sku_id");
                RequireInteger(ctx, inv, ip, "quantity", min: 1);
                RequireString(ctx, inv, ip, "location_id");
                RequireStatus(ctx, inv, ip, "status");
            }

            i++;
        }
    }

    private static void ValidateExpected(Ctx ctx, JsonElement e)
    {
        const string p = "$.expected";
        if (e.ValueKind != JsonValueKind.Object)
        {
            ctx.NotObject(p, "expected 期望验收", e);
            return;
        }

        // Every expected field is optional, but each present value must be a
        // non-negative integer. This validates the shape only; it does not compare
        // against an actual run-artifact (out of scope for input validation).
        foreach (var name in ExpectedFields)
        {
            OptionalInteger(ctx, e, p, name, min: 0);
        }
    }

    private static readonly string[] ExpectedFields =
    {
        "started_at_ms", "finished_at_ms", "completed_receipts",
        "completed_outbound_orders", "completed_each_pick_orders",
        "total_quantity_available", "total_quantity_shipped", "total_quantity_picked",
        "final_world_time_ms", "event_log_line_count",
    };

    // ---- field helpers -------------------------------------------------------

    private static void RequireString(Ctx ctx, JsonElement parent, string path, string name)
    {
        if (!parent.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            ctx.Missing(path, name);
            return;
        }

        var cp = Child(path, name);
        if (v.ValueKind != JsonValueKind.String)
        {
            ctx.TypeError(cp, name, JsonValueKind.String, v);
            return;
        }

        if (string.IsNullOrWhiteSpace(v.GetString()))
        {
            ctx.Add(cp, "string.empty", $"{FieldLabel(name)} 不能为空。");
        }
    }

    private static void OptionalString(Ctx ctx, JsonElement parent, string path, string name)
    {
        // Absent is fine; present-but-null (or any non-string) is a type error,
        // matching scenario.schema.json (a present key must match its type).
        if (!parent.TryGetProperty(name, out var v))
        {
            return;
        }

        if (v.ValueKind != JsonValueKind.String)
        {
            ctx.TypeError(Child(path, name), name, JsonValueKind.String, v);
        }
    }

    private static void RequireInteger(Ctx ctx, JsonElement parent, string path, string name, long min)
    {
        if (!parent.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            ctx.Missing(path, name);
            return;
        }

        ValidateInteger(ctx, Child(path, name), name, v, min);
    }

    private static void OptionalInteger(Ctx ctx, JsonElement parent, string path, string name, long min)
    {
        // Absent is fine; present-but-null (or any non-integer) is reported by
        // ValidateInteger, matching scenario.schema.json.
        if (!parent.TryGetProperty(name, out var v))
        {
            return;
        }

        ValidateInteger(ctx, Child(path, name), name, v, min);
    }

    private static void ValidateInteger(Ctx ctx, string cp, string name, JsonElement v, long min)
    {
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt64(out var n))
        {
            ctx.IntTypeError(cp, name, v);
            return;
        }

        if (n < min)
        {
            ctx.RangeError(cp, name, n, min);
        }
    }

    private static void RequireStatus(Ctx ctx, JsonElement parent, string path, string name)
    {
        if (!parent.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            ctx.Missing(path, name);
            return;
        }

        var cp = Child(path, name);
        if (v.ValueKind != JsonValueKind.String)
        {
            ctx.TypeError(cp, name, JsonValueKind.String, v);
            return;
        }

        var s = v.GetString()!;
        if (!AllowedStatusSet.Contains(s))
        {
            ctx.EnumError(cp, name, s, AllowedInventoryStatusList);
        }
    }

    private static bool RequireObject(Ctx ctx, JsonElement parent, string path, string name, out JsonElement value)
    {
        value = default;
        if (!parent.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            ctx.Missing(path, name);
            return false;
        }

        if (v.ValueKind != JsonValueKind.Object)
        {
            ctx.NotObject(Child(path, name), FieldLabel(name), v);
            return false;
        }

        value = v;
        return true;
    }

    private static bool RequireNonEmptyArray(Ctx ctx, JsonElement parent, string path, string name, out JsonElement value)
    {
        value = default;
        var cp = Child(path, name);
        if (!parent.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            ctx.Add(cp, "field.missing", $"缺少必填字段:{FieldLabel(name)}(应为一个列表)。");
            return false;
        }

        if (v.ValueKind != JsonValueKind.Array)
        {
            ctx.Add(cp, "field.type", $"{FieldLabel(name)} 应为一个列表(用 [ ] 包裹),当前为{KindCn(v.ValueKind)}。");
            return false;
        }

        if (v.GetArrayLength() == 0)
        {
            ctx.Add(cp, "array.empty", $"{FieldLabel(name)} 至少需要一条记录。");
            return false;
        }

        value = v;
        return true;
    }

    private static string Child(string parentPath, string name)
        => parentPath == "$" ? $"$.{name}" : $"{parentPath}.{name}";

    private static string FieldLabel(string name)
        => FieldLabels.TryGetValue(name, out var label) ? label : name;

    private static string KindCn(JsonValueKind kind) => kind switch
    {
        JsonValueKind.String => "文本",
        JsonValueKind.Number => "数字",
        JsonValueKind.True or JsonValueKind.False => "布尔值",
        JsonValueKind.Array => "列表",
        JsonValueKind.Object => "对象",
        JsonValueKind.Null => "空(null)",
        _ => "未知类型",
    };

    private static readonly Dictionary<string, string> FieldLabels = new(StringComparer.Ordinal)
    {
        ["schema_version"] = "契约版本(schema_version)",
        ["scenario_id"] = "场景 ID(scenario_id)",
        ["seed"] = "随机种子(seed)",
        ["description"] = "场景说明(description)",
        ["process"] = "处理流程(process)",
        ["receipts"] = "入库收货列表(receipts)",
        ["orders"] = "订单列表(orders)",
        ["inventory"] = "库存列表(inventory)",
        ["expected"] = "期望验收(expected)",
        ["dock_count"] = "月台数量(dock_count)",
        ["forklift_count"] = "叉车数量(forklift_count)",
        ["worker_count"] = "工人数量(worker_count)",
        ["station_count"] = "工作站数量(station_count)",
        ["receipt_id"] = "收货单号(receipt_id)",
        ["warehouse_id"] = "仓库编号(warehouse_id)",
        ["sku_id"] = "SKU 编号(sku_id)",
        ["order_id"] = "订单号(order_id)",
        ["inventory_id"] = "库存编号(inventory_id)",
        ["dock_id"] = "月台编号(dock_id)",
        ["pick_station_id"] = "拣货工作站(pick_station_id)",
        ["location_id"] = "库位编号(location_id)",
        ["staging_location_id"] = "暂存库位(staging_location_id)",
        ["storage_location_id"] = "存储库位(storage_location_id)",
        ["pick_location_id"] = "拣货库位(pick_location_id)",
        ["pick_face_location_id"] = "拣货面库位(pick_face_location_id)",
        ["quantity"] = "数量(quantity)",
        ["status"] = "库存状态(status)",
        ["arrives_at_ms"] = "到达时间(arrives_at_ms,毫秒)",
        ["released_at_ms"] = "释放时间(released_at_ms,毫秒)",
        ["unload_duration_ms"] = "卸货时长(unload_duration_ms,毫秒)",
        ["qc_duration_ms"] = "质检时长(qc_duration_ms,毫秒)",
        ["putaway_duration_ms"] = "上架时长(putaway_duration_ms,毫秒)",
        ["pick_duration_ms"] = "拣货时长(pick_duration_ms,毫秒)",
        ["stage_duration_ms"] = "暂存时长(stage_duration_ms,毫秒)",
        ["dock_travel_duration_ms"] = "月台运输时长(dock_travel_duration_ms,毫秒)",
        ["load_duration_ms"] = "装车时长(load_duration_ms,毫秒)",
        ["tote_bind_duration_ms"] = "绑定料箱时长(tote_bind_duration_ms,毫秒)",
        ["travel_to_station_duration_ms"] = "前往工作站时长(travel_to_station_duration_ms,毫秒)",
        ["pick_service_duration_ms"] = "拣选作业时长(pick_service_duration_ms,毫秒)",
        ["move_to_staging_duration_ms"] = "移动至暂存时长(move_to_staging_duration_ms,毫秒)",
        ["started_at_ms"] = "开始时间(started_at_ms,毫秒)",
        ["finished_at_ms"] = "结束时间(finished_at_ms,毫秒)",
        ["completed_receipts"] = "完成的入库收货数(completed_receipts)",
        ["completed_outbound_orders"] = "完成的出库订单数(completed_outbound_orders)",
        ["completed_each_pick_orders"] = "完成的拣选订单数(completed_each_pick_orders)",
        ["total_quantity_available"] = "可用总量(total_quantity_available)",
        ["total_quantity_shipped"] = "出库总量(total_quantity_shipped)",
        ["total_quantity_picked"] = "拣选总量(total_quantity_picked)",
        ["final_world_time_ms"] = "最终世界时间(final_world_time_ms,毫秒)",
        ["event_log_line_count"] = "事件日志行数(event_log_line_count)",
    };

    private sealed class Ctx
    {
        private readonly List<ScenarioValidationError> _errors = new();

        public void Add(string path, string code, string message)
            => _errors.Add(new ScenarioValidationError(path, code, message));

        public ScenarioValidationResult ToResult()
            => new(_errors.Count == 0, _errors);

        public void Missing(string parentPath, string name)
            => Add(Child(parentPath, name), "field.missing", $"缺少必填字段:{FieldLabel(name)}。");

        public void TypeError(string path, string name, JsonValueKind expected, JsonElement actual)
            => Add(path, "field.type", $"{FieldLabel(name)} 应为{KindCn(expected)},当前为{KindCn(actual.ValueKind)}。");

        public void IntTypeError(string path, string name, JsonElement actual)
        {
            var detail = actual.ValueKind switch
            {
                JsonValueKind.Number => $",当前为 {actual.GetRawText()}(整数不能带小数点或科学计数法,也不能超出可处理范围)",
                JsonValueKind.String => ",当前为文本",
                _ => $",当前为{KindCn(actual.ValueKind)}",
            };
            Add(path, "field.type", $"{FieldLabel(name)} 应为整数{detail}。");
        }

        public void RangeError(string path, string name, long value, long min)
        {
            var (code, message) = min switch
            {
                1 => ("value.must_be_positive", $"{FieldLabel(name)} 必须大于 0(当前值:{value})。"),
                0 => ("value.must_be_non_negative", $"{FieldLabel(name)} 不能为负数(当前值:{value})。"),
                _ => ("value.too_small", $"{FieldLabel(name)} 不能小于 {min}(当前值:{value})。"),
            };
            Add(path, code, message);
        }

        public void EnumError(string path, string name, string value, IReadOnlyList<string> allowed)
            => Add(path, "value.invalid_enum",
                $"{FieldLabel(name)}的取值 \"{value}\" 无效;允许的取值:{string.Join("、", allowed)}。");

        public void NotObject(string path, string label, JsonElement actual)
            => Add(path, "field.type", $"{label} 应为一个对象(用 {{ }} 包裹),当前为{KindCn(actual.ValueKind)}。");
    }
}
