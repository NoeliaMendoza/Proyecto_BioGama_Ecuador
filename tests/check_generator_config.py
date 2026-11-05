"""Valida solo la configuracion; no importa ni ejecuta el generador masivo."""
import ast
import os
from pathlib import Path
from random import SystemRandom

source = Path(__file__).resolve().parents[1] / "datos.py"
tree = ast.parse(source.read_text(encoding="utf-8-sig"))
function = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == "load_db_config")
namespace = {"os": os}
exec(compile(ast.Module(body=[function], type_ignores=[]), str(source), "exec"), namespace)
os.environ.pop("PGPASSWORD", None)
try:
    namespace["load_db_config"]()
except RuntimeError:
    pass
else:
    raise AssertionError("La configuracion debe rechazar una clave ausente")

os.environ["PGPASSWORD"] = "test-placeholder"
os.environ["PGHOST"] = "test-db"
os.environ["PGPORT"] = "5544"
config = namespace["load_db_config"]()
assert config["password"] == os.environ["PGPASSWORD"]
assert config["host"] == "test-db"
assert config["port"] == 5544
random_assignment = next(
    node for node in tree.body
    if isinstance(node, ast.Assign)
    and any(isinstance(target, ast.Name) and target.id == "secure_random" for target in node.targets)
)
random_namespace = {"SystemRandom": SystemRandom}
exec(compile(ast.Module(body=[random_assignment], type_ignores=[]), str(source), "exec"), random_namespace)
generator = random_namespace["secure_random"]
assert isinstance(generator, SystemRandom)
for _ in range(20):
    assert generator.choice(["Costa", "Sierra"]) in {"Costa", "Sierra"}
    assert 1 <= generator.randint(1, 200) <= 200
    assert -4 <= generator.uniform(-4, 1) <= 1
    assert 0 <= generator.random() < 1
print("PASS: sintaxis Python y credenciales desde entorno; no se ejecutaron conexiones ni cargas de datos.")
