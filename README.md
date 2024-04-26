# PITMSample
Sample code for PITM (Programmable Item Template Model) data format.

PITM is a glTF-based data format for craft items & acessories in cluster.

* `pitm.proto`: Protocol Buffers definition for glTF exntensions in PITM data format
* `python/`: minimum sample code for Python3
* `unity-Image2Item/`: Unity source code for ["Tool that can create/upload craft items and accessories without Unity" (original Japanese article link)](https://creator.cluster.mu/2023/10/13/image2item/)


## Executing python sample code

Clone the repository, and run the following commands in Linux-like environment.

```shell
cd ./python
pip install -r requirements.txt

# upload an accessory of a pin badge with the given photo, and the thumbnail.
env CCK_ACCESS_TOKEN="abc1234..." python ./main.py upload-badge-accessory ./sample_photo.png ./sample_thumbnail.png

# upload a craft item of a photo frame with the given photo, and the thumbnail.
env CCK_ACCESS_TOKEN="abc1234..." python ./main.py upload-photoframe-item ./sample_photo.png ./sample_thumbnail.png
```

* `main.py`: executable file, uses `PITMWriter` and `UploadApiClient` to generate and upload simple PITMs
* `pitm.py`: contains `PITMWriter` utility
* `cluster_api.py`: contains `UploadApiClient` utility


## Note on .proto generation
In general, `protoc` version and runtime proto library version must match.

We intentionally put `protobuf==3.20.3` in `requirements.txt`, to match the `protoc` version used for included `pitm_pb2.py` file.

If you want to use different version of protobuf, you can re-generate `pitm_pb2.py` by running the following command at the repository root.
```shell
protoc --python_out=./ pitm.proto
mv pitm_pb2.py python/
```
