import base64
import pitm_pb2
import struct
from pygltflib import *


class PITMWriter:
    """Utility class to create a single PITM."""
    def __init__(self):
        self.gltf = GLTF2()
        self.gltf.scenes.append(Scene())
        self.gltf.buffers.append(Buffer())
        self.gltf.extensionsUsed = ["ClusterItem", "ClusterItemNode"]
        self.gltf.scene = 0

        self.buffers = []
        self.bufferOffset = 0
    
    def set_root_node(self, node_ix: int):
        self.gltf.scenes[0].nodes = [node_ix]
    
    def set_parent(self, node_parent_ix: int, node_child_ix: int, trans=[1.0, 1.0, 1.0], rot=[0.0, 0.0, 0.0, 1.0] , scale=[1.0, 1.0, 1.0]):
        self.gltf.nodes[node_parent_ix].children.append(node_child_ix)
        self.gltf.nodes[node_child_ix].translation = trans
        self.gltf.nodes[node_child_ix].rotation = rot
        self.gltf.nodes[node_child_ix].scale = scale
    
    def set_desc(self, desc: pitm_pb2.Item):
        self.gltf.extensions["ClusterItem"] = {"item": url_safe_base64(desc.SerializeToString())}
    
    def add_node(self, desc: pitm_pb2.ItemNode = None, mesh: int = None) -> int:
        """Returns a new Node index that refers to the node """
        node = Node()
        node_ix = len(self.gltf.nodes)
        if desc == None:
            desc = pitm_pb2.ItemNode()
        node.extensions["ClusterItemNode"] = {"itemNode": url_safe_base64(desc.SerializeToString())}
        if mesh != None:
            node.mesh = mesh
        self.gltf.nodes.append(node)
        return node_ix

    def add_bufferview(self, data: bytes, target = None) -> int:
        """Returns a new BufferView index that refers to the data """
        bv_ix = len(self.gltf.bufferViews)

        bv = BufferView()
        bv.buffer = 0
        bv.byteOffset = self.bufferOffset
        bv.byteLength = len(data)
        if target != None:
            bv.target = target
        self.gltf.bufferViews.append(bv)
    
        self.buffers.append(data)
        self.bufferOffset += len(data)

        return bv_ix


    def add_mesh(self, prims) -> int:
        """ Returns a new Mesh index that refers to the primitives """
        mesh_ix = len(self.gltf.meshes)
        mesh = Mesh()
        mesh.primitives = prims
        self.gltf.meshes.append(mesh)
        return mesh_ix

    def add_texture(self, image_blob: bytes, image_mime: str = "image/png") -> int:
        """ Add texture and sampler and returns texture index. """
        sampler_ix = len(self.gltf.samplers)
        sampler = Sampler()
        sampler.magFilter = LINEAR
        sampler.minFilter = LINEAR
        self.gltf.samplers.append(sampler)

        image_ix = len(self.gltf.images)
        image = Image()
        image.bufferView = self.add_bufferview(image_blob)
        image.mimeType = image_mime
        self.gltf.images.append(image)

        texture_ix = len(self.gltf.textures)
        texture = Texture()
        texture.sampler = sampler_ix
        texture.source = image_ix
        self.gltf.textures.append(texture)
        return texture_ix

    def add_material_standard(self, baseColor = [1.0, 1.0, 1.0, 1.0], base_texture: int = None, metallic = 0.0) -> int:
        """ Returns a new Material index with specified baseColor (r,g,b,a) [0,1] """
        mat_ix = len(self.gltf.materials)
        mat = Material()
        pbr = PbrMetallicRoughness()
        if len(baseColor) == 3:
            baseColor = baseColor + [1.0]
        pbr.baseColorFactor = baseColor
        pbr.metallicFactor = metallic
        if base_texture != None:
            texInfo = TextureInfo()
            texInfo.index = base_texture
            pbr.baseColorTexture = texInfo
        mat.pbrMetallicRoughness = pbr
        self.gltf.materials.append(mat)
        return mat_ix

    def add_material_mtoon(self, baseColor = [1.0, 1.0, 1.0, 1.0], base_texture: int = None) -> int:
        mat_ix = len(self.gltf.materials)
        mat = Material()
        mtoon = {
            "shader": "VRM/MToon",
            "renderQueue": 2000,
            "keywordMap": {"_EMISSION": False},
            "tagMap": {"RenderType": "Opaque"},
            "floatProperties": {},
            "textureProperties": {},
            "vectorProperties": {},
        }
        if base_texture != None:
            mtoon["textureProperties"]["_MainTex"] = base_texture

        mtoon["vectorProperties"]["_Color"] = baseColor
        mtoon["vectorProperties"]["_ShadeColor"] = baseColor
        mat.extras["ClusterVRM0MToon"] = {"MToonMat": mtoon}
        self.gltf.materials.append(mat)
        return mat_ix


    def create_prim_tris(self, indices, vertices, material_ix: int = None, uvs = None) -> Primitive:
        """Returns a new Primitive with newly created index & vertex buffers.

        indices -- triangles (counter-clockwise) [ix00, ix01, ix02,  ix10, ix11, ix12,  ...]
        vertices -- vertex positions [x0, y0, z0,  x1, y1, z1,  ...]
        uvs -- vertex uvs [u0, v0,  u1, v1,  ...]
        """

        bv_indices = self.add_bufferview(struct.pack(f"<{len(indices)}H", *indices), ELEMENT_ARRAY_BUFFER)
        ac_indices_ix = len(self.gltf.accessors)
        ac_indices = Accessor()
        ac_indices.bufferView = bv_indices
        ac_indices.type = SCALAR
        ac_indices.componentType = UNSIGNED_SHORT
        ac_indices.count = len(indices)
        self.gltf.accessors.append(ac_indices)

        if len(vertices) % 3 != 0:
            raise ValueError("len of vertices must be multiple of 3")
        bv_vertices = self.add_bufferview(struct.pack(f"<{len(vertices)}f", *vertices), ARRAY_BUFFER)
        ac_vertices_ix = len(self.gltf.accessors)
        ac_vertices = Accessor()
        ac_vertices.bufferView = bv_vertices
        ac_vertices.type = VEC3
        ac_vertices.componentType = FLOAT
        ac_vertices.count = len(vertices) // 3
        ac_vertices.min = [min(vertices[i::3]) for i in range(3)]
        ac_vertices.max = [max(vertices[i::3]) for i in range(3)]
        self.gltf.accessors.append(ac_vertices)

        ac_uvs_ix = None
        if uvs != None:
            bv_uvs = self.add_bufferview(struct.pack(f"<{len(uvs)}f", *uvs), ARRAY_BUFFER)
            ac_uvs_ix = len(self.gltf.accessors)
            ac_uvs = Accessor()
            ac_uvs.bufferView = bv_uvs
            ac_uvs.type = VEC2
            ac_uvs.componentType = FLOAT
            ac_uvs.count = len(uvs) // 2
            self.gltf.accessors.append(ac_uvs)

        prim = Primitive()
        prim.mode = TRIANGLES
        prim.indices = ac_indices_ix
        prim.attributes.POSITION = ac_vertices_ix
        if ac_uvs_ix != None:
            prim.attributes.TEXCOORD_0 = ac_uvs_ix
        if material_ix != None:
            prim.material = material_ix
        return prim


    def get_glb(self) -> bytes:
        """Returns the PITM blob. After calling this, the ItemWriter instance is no longer usable."""
        self.gltf.set_binary_blob(b''.join(self.buffers))
        return b''.join(self.gltf.save_to_bytes())


def url_safe_base64(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).decode().replace("=", "")
