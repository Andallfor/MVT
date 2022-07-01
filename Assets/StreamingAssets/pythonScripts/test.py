import time
import sys

# ALL PRINT STATMENTS MUST HAVE flush=True
print(f"received args {sys.argv}", flush=True)

for i in range(10):
    print(i, flush=True)
    time.sleep(0.5)

print(i/0, flush=True)

# the last thing outputed by the program is always 'Null'