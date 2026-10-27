#!/usr/bin/env python3
"""Static audit of SmartCraft recipe-pruning rules.

This does not replace in-game testing, but catches rule/catalog drift before
a client or server DLL is built.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent
catalog = json.loads((root / "installed-mod-bundles.v1.json").read_text())
policy = json.loads((root / "recipe-preferences.v1.json").read_text())
source = (root.parent / "shared" / "RecipeCleanupPolicy.cs").read_text()
bundles = [tuple(row) for group in catalog["sources"].values() for row in group["items"]]
assert len(bundles) == 451
assert len({row[0] for row in bundles}) == 451

rules = policy["inferior_bundle_outputs"]
assert len(rules) == 20
ids = [r["inferior"] for r in rules]
assert len(ids) == len(set(ids))
byid = {id_: (base, units) for id_, base, units in bundles}
for rule in rules:
    id_, base, units = rule["inferior"], rule["family"], rule["units"]
    assert byid[id_] == (base, units)
    width, height = rule["size"]
    assert width * height > min(w * h for w, h in rule["best_size"])
    for winner in rule["preferred_ids"]:
        assert byid[winner] == (base, units), (id_, winner)
assert policy["explicit_suppression"] == [{
    "input": [[72, 1]], "output": [[67, 3]],
    "reason": "Requested inefficient vanilla Can salvage. Preserves 4 Scrap -> Can."
}]

section = source.split("InferiorOutputs = new HashSet<ushort>", 1)[1]
suppressed_text = section.split("{", 1)[1].split("}", 1)[0]
compiled_ids = {int(x) for x in re.findall(r"\b\d+\b", suppressed_text)}
assert compiled_ids == set(ids), (compiled_ids ^ set(ids))
assert 'ingredient.Units > result.Units' in source, "Unstack paths must remain"
print("PASS: 20 inferior recipe targets, canonical winners, Can salvage exception and C# policy in sync")
