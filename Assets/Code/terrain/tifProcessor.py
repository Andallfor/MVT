import matplotlib.pyplot as plt
from osgeo import gdal
import numpy as np
from skimage.transform import resize

def debugArea(x, y, w, h, src):
    b = np.zeros((h, w))
    for i in range(w):
        for j in range(h):
            d = src[y+j, x+i]
            if np.isnan(d):
                b[j, i] = np.nan
            else:
                b[j, i] = int(d)
    
    return b

def createBox(x, y, w, h, log=False):
    global drawingIndex
    
    bottom = (~np.isnan(data[y+h+2, x:x+w+1])).all()
    right = (~np.isnan(data[y:y+h+1, x+w+2])).all()
    
    if bottom and right:
        box = data[y:y+h+2, x:x+w+2]
        newBounds = (h // scale + 2, w // scale + 2)
    elif bottom:
        box = data[y:y+h+2, x:x+w+1]
        newBounds = (h // scale + 2, w // scale + 1)
    elif right:
        box = data[y:y+h+1, x:x+w+2]
        newBounds = (h // scale + 1, w // scale + 2)
    else:
        box = data[y:y+h+1, x:x+w+1]
        newBounds = (h // scale + 1, w // scale + 1)

    if log:
        n = np.isnan(box).any()
        if n:
            print(f'{y} {x}: {n}')
            print("WARNING: NAN DETECTED")
    filledArea[y:y+h+1, x:x+w+1] = np.ones((h+1, w+1))
    name = f'{x}x{y}_{w}x{h}'
    np.save(f'files/{name}', resize(box, newBounds))

tiff = gdal.Open("C:/Users/leozw/Downloads/ldem_87s_5mpp.tif")

data = tiff.GetRasterBand(1).ReadAsArray()
filledArea = np.full(data.shape, np.nan)

scale = 5
lengthPerCommonBlock = 3600

createBox(lengthPerCommonBlock + 10,     lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock + 10,     lengthPerCommonBlock, lengthPerCommonBlock)
createBox(lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock - 10,     lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock + 10,     lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock * 2 + 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(lengthPerCommonBlock + 10,     -lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(lengthPerCommonBlock * 2 + 10, -lengthPerCommonBlock + 39999 - lengthPerCommonBlock - 10,     lengthPerCommonBlock, lengthPerCommonBlock)
createBox(lengthPerCommonBlock * 2 + 10, -lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock - 10,     -lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, -lengthPerCommonBlock + 39999 - lengthPerCommonBlock - 10,     lengthPerCommonBlock, lengthPerCommonBlock)
createBox(-lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, -lengthPerCommonBlock + 39999 - lengthPerCommonBlock * 2 - 10, lengthPerCommonBlock, lengthPerCommonBlock)

mainLength = 18379
offset = 10810
lengths = [3063, 3063, 3063, 3063, 3063, 3064]
for i in range(3):
    for j in range(6):
        createBox(offset + sum(lengths[:j]), i * lengthPerCommonBlock + 10,                                lengths[j], lengthPerCommonBlock, True)
        createBox(offset + sum(lengths[:j]), 39999 - i * lengthPerCommonBlock - 10 - lengthPerCommonBlock, lengths[j], lengthPerCommonBlock, True)
        createBox(i * lengthPerCommonBlock + 10, offset + sum(lengths[:j]),                                lengthPerCommonBlock, lengths[j], True)
        createBox(39999 - i * lengthPerCommonBlock - 10 - lengthPerCommonBlock, offset + sum(lengths[:j]), lengthPerCommonBlock, lengths[j], True)

for i in range(6):
    for j in range(6):
        createBox(offset + sum(lengths[:i]), offset + sum(lengths[:j]), lengths[i], lengths[j], True)

#plt.imshow(rescale(filledArea, 0.1, anti_aliasing=False))
#plt.imshow(rescale(data, 0.1, anti_aliasing=False))

_filledArea = np.isnan(filledArea)
_data = np.isnan(data)

comparison = _filledArea == _data
print(np.count_nonzero(~comparison))

#plt.imshow(rescale(comparison, 0.1, anti_aliasing=False))

#plt.show()