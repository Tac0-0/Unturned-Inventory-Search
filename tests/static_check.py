"""Dependency-free project/package sanity check. Does not compile or run Unturned."""
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
source = (root / 'src' / 'InventorySearchPlugin.cs').read_text(encoding='utf-8')
project = ET.parse(root / 'InventorySearchPrototype.csproj')
assert project.getroot().attrib['Sdk'] == 'Microsoft.NET.Sdk'
assert project.getroot().find('.//Reference[@Include="SDG.Glazier.Runtime"]') is not None
assert project.getroot().find('.//Reference[@Include="UnityEngine.TextRenderingModule"]') is not None
assert 'BepInPlugin(' in source
assert 'CreateStringField()' in source
assert 'FocusControl()' in source
assert 'Texture2D.whiteTexture' in source
assert 'SleekItems[]' in source
assert 'DimOpacity = 0.75f' in source
assert 'InventorySearch.Tick running' in source
assert 'HOOK FIRED: PlayerUI.Update' in source
assert 'OnInventoryOpen' in source
assert 'OnInventoryConstructed' in source
assert 'OnDashboardConstructed' in source
assert 'AccessTools.Constructor(typeof(PlayerDashboardInventoryUI), Type.EmptyTypes)' in source
assert 'AttachFromHook' in source
assert 'Inventory Search Persistent Runner' in source
assert 'persistentRunnerObject.AddComponent<InventorySearchRuntimeDriver>()' in source
assert 'DontDestroyOnLoad(persistentRunnerObject)' in source
assert 'RUNNER UPDATE FIRED' in source
assert 'InventorySearchPlugin.TickFromRunner()' in source
assert 'appPlaying=' in source
assert project.getroot().find('.//Reference[@Include="0Harmony"]') is not None
assert 'Local player detected' in source
assert 'UI flags:' in source
assert 'sendDragItem' not in source
assert 'sendDropItem' not in source
assert 'sendSwapItem' not in source
assert (root / 'scripts' / 'Build-And-Install.ps1').exists()
print('Static smoke checks passed. Runtime, compile and drag behavior still UNVERIFIED.')