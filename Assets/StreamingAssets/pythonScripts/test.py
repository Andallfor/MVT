import time
import sys
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
HOW TO USE PIP (windows):

<path to unity project>/Assets/StreamingAssets/pythonWindows/Scripts/pip.exe install <name of module>
'''