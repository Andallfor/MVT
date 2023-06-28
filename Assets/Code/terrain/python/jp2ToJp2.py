import glymur

INPUT = "C:/Users/leozw/Documents/GitHub/MVT/Assets/StreamingAssets/terrain/facilities/earth/svalbard/data.jp2"
OUTPUT = "C:/Users/leozw/Documents/GitHub/MVT/Assets/StreamingAssets/terrain/facilities/earth/svalbard/data2.jp2"

imgIn = glymur.Jp2k(INPUT)
glymur.Jp2k(OUTPUT, data=imgIn[:], cratios=[1024, 512, 256, 128, 64, 32, 16, 8, 4, 2, 1])

print("done")