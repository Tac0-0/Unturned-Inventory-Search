#!/usr/bin/env python3
"""Static SmartCraft v1.4 item-equivalence catalog regression checks.

These tests validate source-data integrity only. They do not assert that the
Unturned client or server already supports these substitutions in gameplay.
"""
import json
from collections import Counter
from pathlib import Path

p = Path(__file__).with_name('installed-mod-bundles.v1.json')
raw = json.loads(p.read_text(encoding='utf8'))
assert raw['schema'] == 1
entries = [(id_, base, units, src) for src, body in raw['sources'].items()
           for id_, base, units in body['items']]
assert len(entries) == 451
assert len({row[0] for row in entries}) == len(entries)
assert len({row[1] for row in entries}) == 207
assert all(id_ != base and 1 < units <= 100 for id_, base, units, _ in entries)
counts = Counter(source for *_, source in entries)
assert counts == Counter({'more-stacking': 416, 'more-farming': 19, 'vanilla': 16})
by_item = {id_: (base, units) for id_, base, units, src in entries}
assert {item: by_item[item] for item in (1909,10162,10163,49101,49102)} == {
    1909:(67,10),10162:(67,10),10163:(67,100),49101:(67,10),49102:(67,100)
}
assert by_item[1900] == (39,12)  # Maple logs in native x12 stack
assert by_item[1910] == (68,5)   # Metal sheets in native x5 stack
assert not any(item in by_item for item in (14732,14733))
assert len(raw['excluded']) == 2

def total_base_units(inventory):
    result = Counter()
    for item_id, jars in inventory.items():
        base, units = by_item.get(item_id, (item_id, 1))
        result[base] += jars * units
    return result

mixed = total_base_units({67:11,49101:1,10163:1,1909:1,66:2,49103:1,1900:1})
assert mixed[67] == 131  # 11 + 10 + 100 + 10
assert mixed[66] == 12   # 2 loose + 10 cloths
assert mixed[39] == 12   # 1 native stack of 12 Maple Logs

# A recipe can require multiple independent ingredient families; the catalog
# must never count units of one as satisfying another.
recipe = {67:4,66:6,39:2}
assert all(mixed[base] >= required for base, required in recipe.items())
assert mixed[67] - recipe[67] == 127
assert mixed[66] - recipe[66] == 6
assert mixed[39] - recipe[39] == 10
print('PASS: 451 variants / 207 families; cross-provider, mixed-denomination and multi-family balance checks')
