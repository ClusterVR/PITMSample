import os
import pitm
import pitm_pb2
import cluster_api
import pygltflib
import sys

def read_binary_file(file_path):
    with open(file_path, 'rb') as file:
        file_bytes = file.read()
    return file_bytes

def create_primitive_quad(iw: pitm.PITMWriter, material_ix: int) -> pygltflib.Primitive:
    """Create a primitive of a quad (1x1, X-Y plane) centered at origin. (with UVs)
    """
    #   ^ y
    # 3 | 2
    # --*----> x
    # 0 | 1
    vertices = [-0.5, -0.5, 0,  0.5, -0.5, 0,  0.5, 0.5, 0,  -0.5, 0.5, 0]
    uvs = [0, 1,  1, 1,  1, 0,  0, 0]
    indices = [0, 1, 2, 0, 2, 3]
    return iw.create_prim_tris(indices, vertices, material_ix, uvs)


def create_primitive_cube(iw: pitm.PITMWriter, material_ix: int) -> pygltflib.Primitive:
    """Create a primitive of a cube (1x1x1) centered at origin. (No UVs)
    """
    vertices = [
        -0.5, -0.5, -0.5,  0.5, -0.5, -0.5,  -0.5, 0.5, -0.5,  0.5, 0.5, -0.5,
        -0.5, -0.5,  0.5,  0.5, -0.5,  0.5,  -0.5, 0.5,  0.5,  0.5, 0.5,  0.5]
    indices = [
        0, 3, 1,  0, 2, 3,  # Z- face
        0, 1, 5,  0, 5, 4,  # Y- face
        0, 6, 2,  0, 4, 6,  # X- face
        7, 6, 4,  7, 4, 5,  # Z+ face
        7, 3, 2,  7, 2, 6,  # Y+ face
        7, 5, 1,  7, 1, 3,  # X+ face
    ]
    return iw.create_prim_tris(indices, vertices, material_ix)


def create_sample_craft_item(photo_png_blob: bytes) -> bytes:
    """Create a craft item of a photo frame.

    photo_png_blob: PNG blob of the photo inside the photo frame
    Returns: PITM blob
    """
    FRAME_COLOR = [0.5, 0.3, 0.2]
    FRAME_SIZE_W = 0.5
    FRAME_SIZE_H = 0.5
    SIZE_RATIO = 0.9  # ratio of photo to frame
    FRAME_THICKNESS = 0.08
    COLLIDER_THICKNESS = 0.15

    iw = pitm.PITMWriter()

    # photo node
    tex_photo = iw.add_texture(photo_png_blob)
    mat_photo = iw.add_material_standard(base_texture=tex_photo, metallic=0.3)
    prim_photo = create_primitive_quad(iw, mat_photo)
    mesh_photo = iw.add_mesh([prim_photo])
    node_photo = iw.add_node(mesh=mesh_photo)

    # frame node
    mat_wood = iw.add_material_standard(FRAME_COLOR)
    prim_frame = create_primitive_cube(iw, mat_wood)
    mesh_frame = iw.add_mesh([prim_frame])
    node_frame = iw.add_node(mesh=mesh_frame)

    # root node
    desc_root = pitm_pb2.ItemNode()
    item_select_shape = desc_root.item_select_shapes.add()
    item_select_shape.shape.box.center.elements.extend([0.0, 0.0, COLLIDER_THICKNESS / 2])
    item_select_shape.shape.box.size.elements.extend([FRAME_SIZE_W, FRAME_SIZE_H, COLLIDER_THICKNESS])
    node_root = iw.add_node(desc=desc_root)

    # asesemble
    iw.set_root_node(node_root)
    iw.set_parent(node_root, node_photo, scale=[FRAME_SIZE_W * SIZE_RATIO, FRAME_SIZE_H * SIZE_RATIO, 1.0], trans=[0.0, 0.0, FRAME_THICKNESS + 1e-2])
    iw.set_parent(node_root, node_frame, scale=[FRAME_SIZE_W, FRAME_SIZE_H, FRAME_THICKNESS], trans=[0.0, 0.0, FRAME_THICKNESS / 2])

    # metadata
    item = pitm_pb2.Item()
    item.meta.size.extend([1, 1, 0])
    ja_name = item.meta.name.add()
    en_name = item.meta.name.add()
    en_name.lang_code = "en"
    en_name.text = "Photo Frame"
    ja_name.lang_code = "ja"
    ja_name.text = "フォトフレーム"
    iw.set_desc(item)

    return iw.get_glb()


def create_sample_accessory(image_png_blob: bytes) -> bytes:
    """Create an accessory of a square pin badge.

    image_png_blob: PNG blob to be painted to the pin badge
    Returns: PITM blob
    """

    BADGE_SIZE = 0.05

    iw = pitm.PITMWriter()

    # badge node
    tex_image = iw.add_texture(image_png_blob)
    mat_badge = iw.add_material_mtoon(base_texture=tex_image)
    prim_badge = create_primitive_quad(iw, mat_badge)
    mesh_badge = iw.add_mesh([prim_badge])
    node_badge = iw.add_node(mesh=mesh_badge)

    # root node
    desc_root = pitm_pb2.ItemNode()
    node_root = iw.add_node(desc=desc_root)

    # assemble
    iw.set_root_node(node_root)
    iw.set_parent(node_root, node_badge, scale=[BADGE_SIZE, BADGE_SIZE, 1.0], trans=[0.0, 0.0, 0.0])

    # metadata
    item = pitm_pb2.Item()
    item.accessory_item.default_offset_transform.translation_rotation_scale.extend([0.05, 0.1, 0.12, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0])
    item.accessory_item.attach_case_to_avatar.default_human_body_bone_name = "Head"
    ja_name = item.meta.name.add()
    en_name = item.meta.name.add()
    en_name.lang_code = "en"
    en_name.text = "Pin Badge"
    ja_name.lang_code = "ja"
    ja_name.text = "缶バッジ"
    iw.set_desc(item)

    return iw.get_glb()


if __name__ == "__main__":
    access_token = os.getenv("CCK_ACCESS_TOKEN")
    if not access_token:
        raise RuntimeError("Environmental variable CCK_ACCESS_TOKEN is missing")
    if len(sys.argv) != 4:
        raise RuntimeError("Invalid command format\nmain.py (upload-badge-accessory|upload-photoframe-item) photo_path thumbnail_path")
    
    command, photo_path, thumb_path = sys.argv[1:]
    photo_blob = read_binary_file(photo_path)
    thumb_blob = read_binary_file(thumb_path)

    client = cluster_api.UploadApiClient(access_token)
    if command == "upload-badge-accessory":
        pitm_blob = create_sample_accessory(photo_blob)
        acc_id = client.upload_accessory(thumb_blob, pitm_blob)
        print("AccessoryTemplateID: " + acc_id)
    elif command == "upload-photoframe-item":
        pitm_blob = create_sample_craft_item(photo_blob)
        item_id = client.upload_craft_item(thumb_blob, pitm_blob)
        print("ItemTemplateID: " + item_id)
    else:
        raise RuntimeError("Invalid command: " + command)
