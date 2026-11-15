"""Run with Blender --background SOURCE.blend --python Tools/Rocks/export_rocks.py.

Exports evaluated mesh objects separately; never saves or modifies the source file.
"""
import bpy
import json
from pathlib import Path
from mathutils import Matrix

root = Path(__file__).resolve().parents[2]
output = root / "Assets/Models/Rocks"
output.mkdir(parents=True, exist_ok=True)
report = []
depsgraph = bpy.context.evaluated_depsgraph_get()
objects = [obj for obj in bpy.context.scene.objects
           if obj.type == "MESH" and obj.name.startswith(("SM_ROCK_", "COL_ROCK_"))]
for index, obj in enumerate(sorted(objects, key=lambda item: item.name)):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=depsgraph)
    # Preserve shape/scale, but remove source scene layout and arbitrary pivots.
    mesh.transform(obj.matrix_world)
    render_name = obj.name.replace("COL_ROCK_", "SM_ROCK_").removesuffix("_Convex")
    reference = bpy.data.objects[render_name]
    reference_mesh = bpy.data.meshes.new_from_object(reference.evaluated_get(depsgraph), depsgraph=depsgraph)
    reference_mesh.transform(reference.matrix_world)
    minimum = [min(v.co[axis] for v in reference_mesh.vertices) for axis in range(3)]
    maximum = [max(v.co[axis] for v in reference_mesh.vertices) for axis in range(3)]
    center = [(minimum[axis] + maximum[axis]) / 2 for axis in range(3)]
    diameter = max(maximum[axis] - minimum[axis] for axis in range(3))
    if diameter <= 0 or len(mesh.polygons) == 0:
        continue
    mesh.transform(Matrix.Translation((-center[0], -center[1], -center[2])))
    mesh.transform(Matrix.Scale(1 / diameter, 4))
    export_obj = bpy.data.objects.new(obj.name, mesh)
    bpy.context.collection.objects.link(export_obj)
    bpy.ops.object.select_all(action="DESELECT")
    export_obj.select_set(True)
    bpy.context.view_layer.objects.active = export_obj
    path = output / f"{obj.name}.fbx"
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
                             bake_anim=False, use_mesh_modifiers=True)
    report.append(dict(source=obj.name, asset=str(path.relative_to(root)).replace("\\", "/"),
                       vertices=len(mesh.vertices), polygons=len(mesh.polygons),
                       sourceDiameter=diameter))
    bpy.data.objects.remove(export_obj, do_unlink=True)
    bpy.data.meshes.remove(reference_mesh)
for image in bpy.data.images:
    if not image.name.startswith("T_ROCK_"):
        continue
    if image.packed_file:
        (output / f"{image.name}.png").write_bytes(image.packed_file.data)
    else:
        image.filepath_raw = str(output / f"{image.name}.png")
        image.file_format = "PNG"
        image.save()
(output / "source-manifest.json").write_text(json.dumps(report, indent=2) + "\n")
print("ROCK_EXPORT_REPORT=" + json.dumps(report))
