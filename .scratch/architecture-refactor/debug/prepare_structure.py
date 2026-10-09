"""One-time text migration; retained as a record, not part of the product."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[2]
models = root / 'Assets/Scripts/Vision/PhotoModels.cs'
text = models.read_text(encoding='utf-8-sig')
primitive_start = text.index('    public readonly struct ImagePoint')
frame_start = text.index('    // Rows start at the top left.')
evidence_start = text.index('    public sealed class BoardObservation')
vision_start = text.index('namespace BrickDefense.Vision')
core_close = text.rfind('}', 0, vision_start)
evidence = text[primitive_start:frame_start] + text[evidence_start:core_close]
(root / 'Assets/Scripts/Contracts/PhotoEvidence.cs').write_text(
    'namespace BrickDefense.Core\n{\n'+evidence+'}\n',encoding='utf-8')
models.write_text(text[:primitive_start]+text[frame_start:evidence_start]+'}\n\n'+text[vision_start:],encoding='utf-8')

assemblies = {
    'Assets/Scripts/Contracts': ('BrickDefense.Contracts', [], True),
    'Assets/ThirdParty': ('BrickDefense.Algorithms', ['BrickDefense.Contracts'], True),
    'Assets/Scripts/Domain': ('BrickDefense.Domain', ['BrickDefense.Contracts','BrickDefense.Algorithms'], True),
    'Assets/Scripts/Vision': ('BrickDefense.Vision', ['BrickDefense.Contracts','BrickDefense.Algorithms'], True),
    'Assets/Scripts/Application': ('BrickDefense.Application', ['BrickDefense.Contracts','BrickDefense.Domain','BrickDefense.Vision'], True),
    'Assets/Scripts/Infrastructure': ('BrickDefense.Infrastructure', ['BrickDefense.Contracts','BrickDefense.Vision'], False),
    'Assets/Scripts/Presentation': ('BrickDefense.Presentation', ['BrickDefense.Contracts','BrickDefense.Application','BrickDefense.Vision','BrickDefense.Infrastructure'], False),
    'Assets/Editor': ('BrickDefense.Editor', ['BrickDefense.Contracts','BrickDefense.Domain','BrickDefense.Vision','BrickDefense.Application','BrickDefense.Infrastructure','BrickDefense.Presentation'], False),
}
for directory,(name,references,pure) in assemblies.items():
    data = dict(name=name,references=references,noEngineReferences=pure)
    if directory == 'Assets/Editor': data['includePlatforms'] = ['Editor']
    (root / directory / (name+'.asmdef')).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')

pixels = root / 'Assets/Scripts/Infrastructure/PhotoPixels.cs'
pixels.write_text(pixels.read_text(encoding='utf-8-sig').replace('namespace BrickDefense.Demo','namespace BrickDefense.Infrastructure'),encoding='utf-8')
scene = root / 'Assets/Scenes/BrickDefenseDemo.unity'
scene.write_text(scene.read_text(encoding='utf-8-sig').replace(
    'Assembly-CSharp::BrickDefense.Demo.BrickDefenseDemo','BrickDefense.Presentation::BrickDefense.Demo.BrickDefenseDemo'),encoding='utf-8')

project = root / 'Tests/BrickDefense.Checks.csproj'
project.write_text(project.read_text(encoding='utf-8-sig').replace('Scripts/Core/*.cs','Scripts/Domain/*.cs').replace(
    '<Compile Include="../Assets/Scripts/Vision/*.cs"',
    '<Compile Include="../Assets/Scripts/Contracts/*.cs" />\n    <Compile Include="../Assets/Scripts/Application/*.cs" />\n    <Compile Include="../Assets/Scripts/Vision/*.cs"'),encoding='utf-8')
print('Architecture structure and assemblies prepared.')
