from math import asin, atan2, cos, degrees, radians, sin

def get_point_at_distance(lat1, lon1, d, bearing, R=6371):
    """
    https://stackoverflow.com/questions/7222382/get-lat-long-given-current-point-distance-and-bearing
    lat: initial latitude, in degrees
    lon: initial longitude, in degrees
    d: target distance from initial
    bearing: (true) heading in degrees
    R: optional radius of sphere, defaults to mean radius of earth

    Returns new lat/lon coordinate {d}km from initial, in degrees
    """
    lat1 = radians(lat1)
    lon1 = radians(lon1)
    a = radians(bearing)
    lat2 = asin(sin(lat1) * cos(d/R) + cos(lat1) * sin(d/R) * cos(a))
    lon2 = lon1 + atan2(
        sin(a) * sin(d/R) * cos(lat1),
        cos(d/R) - sin(lat1) * sin(lat2)
    )
    return (degrees(lat2), degrees(lon2),)

def getURL(point, label):
    minY, minX = get_point_at_distance(point[0], point[1], 28.2742712475 * 1.25, 225)
    maxY, maxX = get_point_at_distance(point[0], point[1], 28.2742712475 * 1.25, 45)
    label += ' ' * (29 + 4 - len(label))
    return f'{label}|    https://portal.opentopography.org/datasets?minX={minX}&minY={minY}&maxX={maxX}&maxY={maxY}&group=global'

POINTS = [
    #[-33.151478, -70.66830755],
    #[64.858719, -147.85769],
    [64.972734, -147.500907],
    [64.80424, -147.5002],
    #[37.5245, -122.1434],
    #[-25.89088, 27.68424],
    #[67.857127, 20.964325],
    #[-77.839204, 166.667057],
    #[-53.15, -70.9],
    #[78.230839, 15.389439],
    #[1.39616, 103.8343],
    #[-72.0022, 2.0575],
    #[-29.04574, 115.348717],
    [19.013945, -155.663301],
    #[37.9225, -75.4764],
    #[32.5047, -106.6108]
]

LABELS = [
    #'AGO',
    #'ASF',
    'GLC',
    'USA',
    #'BGS',
    #'HBK',
    #'KIR',
    #'MG',
    #'PA',
    #'SG',
    #'SING',
    #'TR',
    #'USD',
    'USH',
    #'WG',
    #'WS'
]

for point, label in zip(POINTS, LABELS):
    print(getURL(point, label))

'''
AGO3  -33.151478   -70.66830755
AGO8  -33.15166666 -70.66722222

ASF1  64.858719    -147.85769
ASF2  64.859475    -147.84952
ASF3  64.8589      -147.8541

GLC   64.972734    -147.500907

USA1  64.80424     -147.5002
USA3  64.8047      -147.5042
USA4  64.8047      -147.5042
USA5  64.8034      -147.5006

BGS   37.5245      -122.1434

HBK   -25.89088    27.68424

KIR   67.857127    20.964325

MG1   -77.839204   166.667057

PA1   -53.15       -70.9

SG1   78.230839    15.389439
SG2   78.230839    15.389439
SG3   78.22972     15.40805
SGKa  78.23277     15.38166

SING  1.39616      103.8343

TR2   -72.0022     2.0575
TR3   -72.001846   2.526201

USD1  -29.04574    115.348717

USH1  19.013945    -155.663301
USH2  19.013772    -155.662903

WG05  37.9225      -75.4764
WG11  37.9249      -75.4765

WS1   32.5047      -106.6108
'''