import glymur

INPUT = "C:/Users/leozw/Documents/GitHub/MVT/Assets/StreamingAssets/terrain/facilities/earth/ASF/data.jp2"
OUTPUT = "C:/Users/leozw/Documents/GitHub/MVT/Assets/StreamingAssets/terrain/facilities/earth/ASF/data2.jp2"

glymur.set_option('lib.num_threads', 8)

imgIn = glymur.Jp2k(INPUT)
glymur.Jp2k(OUTPUT, data=imgIn[:], cratios=[1024, 512, 256, 128, 64, 32, 16, 8, 4, 2, 1])

print("done")