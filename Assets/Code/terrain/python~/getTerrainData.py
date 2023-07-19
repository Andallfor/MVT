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
    minY, minX = get_point_at_distance(point[0], point[1], 70.7106781187, 225)
    maxY, maxX = get_point_at_distance(point[0], point[1], 70.7106781187, 45)
    label += ' ' * (29 + 4 - len(label))
    return f'{label}|    https://portal.opentopography.org/datasets?minX={minX}&minY={minY}&maxX={maxX}&maxY={maxY}&group=global'

POINTS = [
    (64.859475, -147.84952),
    (64.8042, -147.5002),
    (32.3078, -64.7505),
    (-29.04574, 115.348717),
    (-25.890879, 27.686029),
    (28.5, -80.6),
    (67.88955833, 21.06565472),
    (-77.839204, 166.667057),
    (29.0666, -80.913),
    (-33.15110748, -70.66640301),
    (1.39616, 103.8343),
    (19.013945, -155.663301),
    (78.230839, 15.389439),
    (-72.001846, 2.526201),
    (37.92815, -75.475753),
    (47.881193, 11.083697),
    (32.540752, -106.612049),
    (-35.398522, 148.981904),
    (35.337569, -116.875523),
    (40.428729, -4.249054)]

LABELS = [
    'ASF', 
    'Alaska, North Pole',
    'Bermuda',
    'Dongara/Yatharagga, West. Aus',
    'Hartesbeesthoek',
    'Kennedy',
    'Kiruna/Esrange',
    'MGS',
    'Ponce De Leon',
    'AGO',
    'Singapore',
    'South Point Hawaii',
    'SGS',
    'Troll, Antarctica',
    'WGS',
    'Weilheim, Germany',
    'NEN',
    'Canberra',
    'Goldstone',
    'Madrid']

for point, label in zip(POINTS, LABELS):
    print(getURL(point, label))