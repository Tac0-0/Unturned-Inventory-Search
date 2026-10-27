from pathlib import Path
from collections import Counter
import re,json,sys
# Pass the root directory of your Unturned dedicated-server installation.
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path("/home/unturned/U3DS")
w=root/"Servers/SmartCraftTest/Workshop/Steam/content/304930"
items={}
base_root=root/"Bundles/Items"

def fields(p):
 s=p.read_text(errors="replace").lstrip("\ufeff")
 return s,dict(re.findall(r"(?m)^([A-Za-z][A-Za-z0-9_]*)[ \t]+([^\r\n/]+)",s))

for folder in [base_root,w/"3729650953",w/"3818995821"]:
 for p in folder.rglob("*"):
  if not p.is_file() or p.suffix.lower() not in (".dat",".asset") or p.stem.lower() in ("english","japanese") :continue
  s,d=fields(p)
  if "GUID" in d and "ID" in d and d["ID"].strip().isdigit():
   items[d["GUID"].strip().lower()]=int(d["ID"].strip())

catalog={}
errors=[]
def add(id,base,unit,source):
 assert id!=base and unit>1
 if id in catalog and (catalog[id]["base"]!=base or catalog[id]["units"]!=unit):
  errors.append((id,"ambiguous",catalog[id],base,unit));return
 if id not in catalog:catalog[id]={"id":id,"base":base,"units":unit,"source":source}

stack_root=w/"3818995821/Bundles/Items"
chain={}; by_id={}
for p in stack_root.rglob("*.dat"):
 if p.stem.lower() in ("english","japanese"):continue
 s,d=fields(p)
 if "ID" not in d or not d["ID"].strip().isdigit():continue
 id=int(d["ID"].strip());by_id[id]=p
 for i in range(int(d.get("Blueprints","0").strip())):
  pre=f"Blueprint_{i}_"
  try:
   input_id=int(d[pre+"Supply_0_ID"].strip())
   output_id=int(d[pre+"Output_0_ID"].strip())
   output_count=int(d[pre+"Output_0_Amount"].strip())
  except (ValueError,KeyError):continue
  if input_id==id and output_count==10 and output_id!=id:
   chain[id]=output_id

for id,p in by_id.items():
 seen={id}; base=id; mul=1
 while base in chain:
  nxt=chain[base]
  if nxt in seen:errors.append((id,"cycle"));break
  seen.add(nxt);base=nxt;mul*=10
 else:
  if mul in (10,100) and base not in by_id:add(id,base,mul,"more-stacking")
  else:errors.append((id,"unverified stack chain",str(p.relative_to(stack_root))))

# More Farming: exact verified workshop IDs and their constituent original items.
morefarming=[(67,49101,49102),(66,49103,49104),(516,49105,0),(73,49107,0),
 (69,49109,0),(75,49111,0),(70,49113,0),(37,49115,49116),(39,49117,49118),
 (41,49119,49120),(74,49121,49122),(42189,42190,0),(42233,42234,0)]
for base,small,large in morefarming:
 for id,unit in ((small,10),(large,100)):
  if not id:continue
  located=[]
  for p in (w/"3729650953/Supplies").rglob("*.asset"):
   text,d=fields(p)
   if d.get("ID","").strip()==str(id):located.append(p)
  if len(located)!=1:errors.append((id,"missing More Farming asset",len(located)));continue
  add(id,base,unit,"more-farming")

# Official native placeable supplies: only stack items with an explicit x10
# reverse blueprint and asset GUID resolving to a known underlying item.
for p in (root/"Bundles/Items/Barricades").rglob("*.dat"):
 text,d=fields(p)
 id_s=d.get("ID","").strip()
 if not id_s.isdigit() or not (1900<=int(id_s)<=1915):continue
 id=int(id_s)
 match=re.search(r"(?m)^\s*OutputItems\s+\"([0-9a-f]{32}) x (\d+)\"",text,re.I)
 if not match:errors.append((id,"native no unstack output"));continue
 base=items.get(match[1].lower())
 if base is None:errors.append((id,"native output GUID unresolved"));continue
 add(id,base,int(match[2]),"vanilla")

stats=Counter(e["source"] for e in catalog.values())
errors.sort(key=lambda t:str(t))
output={"schema":1,"method":"Verified reverse unstack 1-to-10 chains, plus manually verified More Farming asset families", "families":len(set(e["base"] for e in catalog.values())),"entries":sorted(catalog.values(),key=lambda e:e["id"]), "excluded":errors}
Path(sys.argv[2] if len(sys.argv)>2 else "stack-catalog-v14.json").write_text(json.dumps(output,indent=2)+"\n")
print("CATALOG",len(catalog),"FAMILIES",output["families"],"SOURCES",dict(stats))
print("EXCLUSIONS",len(errors),errors[:18])
print("CROSS_STACKS_SCRAP",[x for x in output["entries"] if x["base"]==67])
