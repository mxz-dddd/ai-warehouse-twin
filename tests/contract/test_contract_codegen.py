import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCHEMA_GLOBS = (
    "packages/contracts/domain/*.schema.json",
    "packages/contracts/events/*.schema.json",
    "packages/contracts/optimization/*.schema.json",
    "packages/contracts/calibration/*.schema.json",
)
REQUIRED_SCHEMA_KEYS = ("$schema", "$id", "title", "type", "properties")
CSharp_OUT = ROOT / "packages/contracts/generated/csharp/Contracts.Generated.cs"
PYTHON_OUT = ROOT / "packages/contracts/generated/python/contracts_generated.py"
MANIFEST_OUT = ROOT / "packages/contracts/generated/manifest.json"


def schema_paths():
    paths = []
    for pattern in SCHEMA_GLOBS:
        paths.extend(ROOT.glob(pattern))
    return sorted(paths, key=lambda path: path.as_posix())


class ContractCodegenTests(unittest.TestCase):
    def test_all_schemas_are_valid_json(self):
        self.assertGreater(len(schema_paths()), 0)
        for path in schema_paths():
            with self.subTest(path=path):
                with path.open("r", encoding="utf-8") as handle:
                    json.load(handle)

    def test_all_schemas_have_required_keys(self):
        for path in schema_paths():
            with self.subTest(path=path):
                with path.open("r", encoding="utf-8") as handle:
                    schema = json.load(handle)
                for key in REQUIRED_SCHEMA_KEYS:
                    self.assertIn(key, schema)

    def test_codegen_outputs_exist_and_include_expected_types(self):
        subprocess.run(
            ["python3", "scripts/generate_contracts.py"],
            cwd=ROOT,
            check=True,
        )

        self.assertTrue(CSharp_OUT.exists())
        self.assertTrue(PYTHON_OUT.exists())
        self.assertTrue(MANIFEST_OUT.exists())

        csharp_text = CSharp_OUT.read_text(encoding="utf-8")
        self.assertIn("namespace WarehouseTwin.Contracts", csharp_text)
        self.assertIn("public sealed record DomainEvent", csharp_text)

        python_text = PYTHON_OUT.read_text(encoding="utf-8")
        self.assertIn("@dataclass(frozen=True)", python_text)
        self.assertIn("class DomainEvent", python_text)

    def test_manifest_contains_all_schema_hashes(self):
        subprocess.run(
            ["python3", "scripts/generate_contracts.py"],
            cwd=ROOT,
            check=True,
        )

        manifest = json.loads(MANIFEST_OUT.read_text(encoding="utf-8"))
        manifest_hashes = {
            item["path"]: item["sha256"]
            for item in manifest["schemas"]
        }

        for path in schema_paths():
            rel_path = path.relative_to(ROOT).as_posix()
            expected_hash = hashlib.sha256(path.read_bytes()).hexdigest()
            with self.subTest(path=rel_path):
                self.assertEqual(manifest_hashes[rel_path], expected_hash)


if __name__ == "__main__":
    unittest.main()
