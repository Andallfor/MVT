import glymur
from os import listdir
from os.path import join
from os.path import split as pSplit
from lxml import etree
import numpy as np
import json

keyIds = {
    34737: 'GeoAsciiParams',
    34736: 'GeoDoubleParams',
    1024: 'GTModelTypeGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.1
    1025: 'GTRasterTypeGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.2
    2048: 'GeographicTypeGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.2.1
    2049: 'GeogCitationGeo', # in 34737
    2050: 'GeogGeodeticDatumGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.2.2
    2054: 'GeogAngularUnitsGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.4
    2056: 'GeogEllipsoidGeo', # http://geotiff.maptools.org/spec/geotiff6.html#6.3.2.3
    2057: 'GeogSemiMajorAxisGeo', # in 34736
    2058: 'GeogSemiMinorAxisGeo', # in 34736
    2061: 'GeogPrimeMeridianLongGeo' # in 34736
}

keyParsers = {
    1024: 'geographic',
    1025: 'pixel as area',
    2048: 'user defined',
    2049: 'GCS Name = GCS_Moon_2000|Datum = Moon_2000|Ellipsoid = Moon_2000_IAU_IAG|Primem = Reference_Meridian',
    2050: 'user defined',
    2054: 'degree',
    2056: 'user defined',
    2057: 1737400.0,
    2058: 1737400.0,
    2061: 0.0
}

glymur.set_option('lib.num_threads', 16)

FOLDER_PATH = "C:/Users/leozw/Downloads/jp2/JP2"
OUTPUT_PATH = "C:/Users/leozw/Desktop/lunar"
RESOLUTIONS = [
    (16, 128),
    (64, 96),
    (64, 64),
    (64, 32),
    (256, 16),
    (256, 8)]

toProcess = set(join(FOLDER_PATH, f)
                .removesuffix('.JP2')
                .removesuffix('_AUX.XML')
                .removesuffix('_JP2.LBL')
                for f in listdir(FOLDER_PATH))

# read and save header info to a json file
for index, filePath in enumerate(toProcess):
    fileName = pSplit(filePath)[1]
    jp2Path = filePath + '.JP2'
    xmlPath = filePath + '_AUX.XML'
    lblPath = filePath + '_JP2.LBL'
    # the files we are parsing have only one band
    jp2 = glymur.Jp2k(jp2Path)
    
    # ================ PARSE HEADER ================
    
    b = jp2.box[3]
    
    data = dict()
    for item in b.data:
        data[item] = b.data[item]
    
    # override specific values
    # this is the same no matter the file, so just hardcode it
    _gkd = data['GeoKeyDirectory']
    geoKeyDirectory = {
        'version': f'{_gkd[0]}.{_gkd[1]}.{_gkd[2]}',
        'numKeys': _gkd[3],
        'keys': [
            {'id': keyIds[_gkd[i]],
            'value': keyParsers[_gkd[i]]}
            for i in range(4, len(_gkd) - 1, 4) if _gkd[i] != 0
        ]
    }
    data['GeoKeyDirectory'] = geoKeyDirectory

    data['width'] = jp2.shape[1]
    data['height'] = jp2.shape[0]
    data['xll'] = data['ModelTiePoint'][3]
    data['yll'] = data['ModelTiePoint'][4] - data['ModelPixelScale'][0] * jp2.shape[0]

    # contained in geokey directory
    del data['GeoDoubleParams']
    del data['GeoAsciiParams']
    
    jsonFile = open(join(OUTPUT_PATH, f'{fileName}.json'), 'w')
    jsonFile.write(json.dumps(data))
    jsonFile.close()

    # ================ PARSE BODY ================
    
    points = jp2[:]
    points = np.array(points)
    with open(join(OUTPUT_PATH, f'{fileName}.npy'), 'wb') as f:
        np.save(f, points)
    
    print(f'Parsed {jp2Path} ({index + 1}/{len(toProcess)})')
