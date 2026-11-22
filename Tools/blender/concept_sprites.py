import bpy, sys, math, os
# optional: CONCEPT_PITCH (deg, default 50 = the game camera), CONCEPT_RES (px, 320), CONCEPT_VIEWS ('yaw:tag,...')
PITCH=float(os.environ.get('CONCEPT_PITCH','50')); RES=int(os.environ.get('CONCEPT_RES','320'))
VIEWS=[(float(v.split(':')[0]),v.split(':')[1]) for v in os.environ['CONCEPT_VIEWS'].split(',')] if os.environ.get('CONCEPT_VIEWS') else ((30,'a'),(150,'b'),(-80,'c'),(80,'d'),(-150,'e'),(0,'f'))
from mathutils import Vector
out = sys.argv[sys.argv.index('--')+1]
R = 'G:/Fork/CityBuilding/Assets/_Project/Models/'
NAMES=sys.argv[sys.argv.index('--')+2].split(',')
for name in NAMES:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=R+name+'.fbx'); name=name.split('/')[-1]
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    img=bpy.data.images.load('G:/Fork/CityBuilding/Assets/_Project/Models/Palette/BuildingsPalette.png')
    for m in {s.material for o in objs for s in o.material_slots if s.material}:
        m.use_nodes=True; nt=m.node_tree; bs=nt.nodes.get('Principled BSDF')
        tx=nt.nodes.new('ShaderNodeTexImage'); tx.image=img; tx.interpolation='Closest'
        nt.links.new(tx.outputs[0], bs.inputs['Base Color']); bs.inputs['Roughness'].default_value=0.9
        if 'Specular IOR Level' in bs.inputs: bs.inputs['Specular IOR Level'].default_value=0.1
    print(name, [(o.name, [m.name for m in o.data.materials]) for o in objs][:6])
    mn=Vector((1e9,)*3); mx=Vector((-1e9,)*3)
    for o in objs:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c); mn=Vector(map(min,mn,w)); mx=Vector(map(max,mx,w))
    ctr=(mn+mx)/2; size=max(mx-mn)
    sc=bpy.context.scene
    cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam)
    cam.data.type='ORTHO'; cam.data.ortho_scale=size*1.25
    for yaw,tag in VIEWS:   # f: straight on, as the game camera
        d=Vector((math.sin(math.radians(yaw))*math.cos(math.radians(PITCH)), -math.cos(math.radians(yaw))*math.cos(math.radians(PITCH)), math.sin(math.radians(PITCH))))
        cam.location=ctr+d*size*4
        cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
        sc.camera=cam
        if not any(o.type=='LIGHT' for o in sc.objects):
            l=bpy.data.objects.new('sun',bpy.data.lights.new('sun','SUN')); l.data.energy=3.5; l.rotation_euler=(math.radians(40),0,math.radians(-30)); sc.collection.objects.link(l)
            sc.world=bpy.data.worlds.new('w'); sc.world.use_nodes=True; sc.world.node_tree.nodes['Background'].inputs[1].default_value=1.2
        sc.render.engine='BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
        sc.render.film_transparent=True; sc.render.resolution_x=sc.render.resolution_y=RES
        sc.view_settings.view_transform='Standard'
        sc.render.filepath=out+'/'+name+'_'+tag+'.png'; bpy.ops.render.render(write_still=True)
