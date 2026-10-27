#!/usr/bin/env python3
"""Ensure the C# snapshot and GUID allowlist contain all verified fruitcrates."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent
catalog=json.loads((root/"installed-mod-bundles.v1.json").read_text())
rows=[tuple(row) for src in catalog["sources"].values() for row in src["items"]]
assert len(rows)==489 and len({id for id,base,units in rows})==489
byid={id:(base,units) for id,base,units in rows}

source=(root.parent/"shared/StackCatalog.cs").read_text()
chunk=source.split("string[] Raw",1)[1].split("};",1)[0]
fragments=re.findall(r'"([0-9:;]+)"',chunk)
parsed={int(id):(int(base),int(units)) for term in ";".join(fragments).split(";")
        if term for id,base,units in [term.split(":")]}
assert parsed==byid,("Catalog C# source mismatch",len(parsed),len(byid))

guidsrc=(root.parent/"shared/KnownStackGuids.cs").read_text()
chunk=guidsrc.split("string[] Parts",1)[1].split("};",1)[0]
parts=re.findall(r'"([0-9a-f:;]+)"',chunk)
guids={int(id):guid for term in ";".join(parts).split(";")
       if term for id,guid in [term.split(":")]}
assert len(guids)==489 and set(guids)==set(byid)
assert guids[48147]=="faac05cd43224cf68183c2d94a62abaa"
assert guids[48177]=="2c3533d300e44f29afdd1c27b63f7c77"
assert byid[48147]==(42213,10) and byid[48177]==(42213,100)
fruits=catalog["sources"]["more-farming-fruitcrates"]["items"]
assert len(fruits)==38 and len({b for _,b,_ in fruits})==19
assert {u for _,_,u in fruits}=={10,100}
assert len({base for _,base,_ in fruits if _})>=1
print("PASS: 489 source/catalog/GUID identities; 38 reversible fruitcrates in 19 families; Strawberry IDs pinned")
