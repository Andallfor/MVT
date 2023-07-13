import matplotlib.pyplot as plt
from math import trunc

def draw(start, end, y, color):
    plt.plot([start, end], [y, y], color=color, linewidth='5')

FILE1 = "C:/Users/AKaze/Downloads/canberra_data.csv"
FILE2 = "C:/Users/AKaze/Downloads/accessNew.txt"

starts = []
ends = []
lengths = []

with open(FILE1, "r") as f:
    for i, line in enumerate(f.readlines()):
        if i > 20:
            break
        _, start, end = [float(i) for i in line.strip().split(',')]

        draw(start, end, 0, 'green')
        
        starts.append(start)
        ends.append(end)
        lengths.append((float(end) - float(start)) * 24 * 60 * 60)

startDifference = 0
endDifference = 0
lengthDifference = 0

count = 0
with open(FILE2, "r") as f:
    for i, line in enumerate(f.readlines()):
        if i >= len(starts):
            break
        _, start, end, length = [float(i) for i in line.strip().split(',')]

        draw(start, end, 1, 'red')
        startDifference += abs(starts[i] - start)
        endDifference += abs(ends[i] - end)
        lengthDifference += abs(lengths[i] - length * 24 * 60 * 60)
        
        lDiff = trunc(lengths[i] - length * 24 * 60 * 60)
        sDiff = trunc((starts[i] - start) * 24 * 60 * 60)
        eDiff = trunc((ends[i] - end) * 24 * 60 * 60)
        print(f'{sDiff} | {eDiff} | {lDiff}')
        
        if (abs(starts[i] - start) > 0.01):
            print(i)

        count += 1

plt.ylim(-10, 10)
plt.show()

print(24 * 60 * 60 * startDifference / count)
print(24 * 60 * 60 * endDifference / count)
print(lengthDifference / count)