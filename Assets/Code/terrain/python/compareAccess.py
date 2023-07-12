import matplotlib.pyplot as plt

def draw(start, end, y, color):
    plt.plot([start, end], [y, y], color=color, linewidth='5')

FILE1 = "C:/Users/leozw/Downloads/accessTruth.csv"
FILE2 = "C:/Users/leozw/Downloads/accessNew.txt"

starts = []
ends = []
lengths = []

with open(FILE1, "r") as f:
    for i, line in enumerate(f.readlines()):
        if i > 20:
            break
        _, start, end, length = [float(i) for i in line.strip().split(',')]

        draw(start, end, 0, 'green')
        
        starts.append(start)
        ends.append(end)
        lengths.append(length)

startDifference = 0
endDifference = 0
lengthDifference = 0

with open(FILE2, "r") as f:
    for i, line in enumerate(f.readlines()):
        if i >= len(starts):
            break
        _, start, end, length = [float(i) for i in line.strip().split(',')]

        draw(start, end, 1, 'red')
        startDifference += abs(starts[i] - start)
        endDifference += abs(ends[i] - end)
        lengthDifference += abs(lengths[i] - length * 24 * 60 * 60)
        print(lengths[i] - length * 24 * 60 * 60)
        
        if (abs(starts[i] - start) > 0.01):
            print(i)

plt.ylim(-10, 10)
plt.show()

print(startDifference / len(starts))
print(endDifference / len(ends))
print(lengthDifference / len(lengths))