import sys
import os
import platform

# include this line in all files, before imports
if 'macos' in platform.platform().lower():
    sys.path.insert(0, os.path.join(os.path.normpath(os.path.dirname(
        __file__) + os.sep + os.pardir), "pythonMac/lib/python3.10/site-packages"))

import math
import datetime
from jplephem.spk import SPK


def get_julian_datetime(date):
    julian_datetime = 367 * date.year - int((7 * (date.year + int((date.month + 9) / 12.0))) / 4.0) + int((275 * date.month) / 9.0) + date.day + 1721013.5 + (
        date.hour + date.minute / 60.0 + date.second / math.pow(60, 2)) / 24.0 - 0.5 * math.copysign(1, 100 * date.year + date.month - 190002.5) + 0.5
    return julian_datetime


def bspReader(startDate, endDate, epoch, path):
    kernel = SPK.open(path)
    positions = []
    time = startDate
    for x in range(startDate, endDate):
        #date = get_julian_datetime(datetime.datetime(2025, 6, x, y, z))
        index = math.floor((time - epoch)/6.94)
        position = (kernel.segments[index].compute(time))
        print([time, position[0], position[1], position[2]], flush=True)
        time += 0.0000116
