# these three are requried
import sys
import os
import platform

# include this line in all files, before imports
if 'macos' in platform.platform().lower():
    sys.path.insert(0, os.path.join(os.path.normpath(os.path.dirname(__file__) + os.sep + os.pardir), "pythonMac/lib/python3.10/site-packages"))

import time
import numpy as np

# ALL PRINT STATMENTS MUST HAVE flush=True
print(f"received args {sys.argv}", flush=True)

for i in range(10):
    print(i, flush=True)
    time.sleep(0.5)

arr = np.array([1, 2, 3, 4])
arr = arr * -1
print(arr, flush=True)

print(i/0, flush=True)

# the last thing outputed by the program is always 'Null'

'''
HOW TO USE PIP:

Windows:
<path to unity project>/Assets/StreamingAssets/pythonWindows/Scripts/pip.exe install <name of module>

Mac:
<path to unity project>/Assets/StreamingAssets/pythonMac/bin/pip3.10 install --target=<path to unity project>/Assets/StreamingAssets/pythonMac/lib/python3.10/site-packages <name of module>
'''