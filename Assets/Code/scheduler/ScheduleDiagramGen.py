import sys
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt


def create_patch(t1, t2, ymin, ymax, value, numColors):
    x1 = [t1, t1, t2, t2]
    y1 = [ymin, ymax, ymax, ymin]
    c = [plt.cm.jet(i)[:3] for i in np.linspace(0,255,numColors+2,dtype=int)[1:-1]]
    if value > 200:
        value = 200
    elif value == 99:
        value = numColors + 1
    h = plt.Polygon(np.column_stack([x1, y1]), facecolor=c[value], edgecolor='black')
    return h


def plotschedule(sched_file):
    df = pd.read_csv(sched_file)
    dim1 = sys.argv[2]
    dim2 = sys.argv[3]
    userNames = df[dim1].unique().tolist()
    gsNames = df[dim2].unique().tolist()
    userNames_dict = {name: i for i, name in enumerate(userNames)}
    gsNames_dict = {name: i for i, name in enumerate(gsNames)}
    yVals = np.arange(1, len(userNames) + 1, 1)
    fig, ax = plt.subplots()
    for i, gsName in enumerate(gsNames):
        h = create_patch(-1, -1, i, i, i, len(gsNames))
        ax.add_patch(h)

    for i, gsName in enumerate(gsNames):
        gsRows = df[df[dim2] == gsName]

        if not gsRows.empty:
            for _, row in gsRows.iterrows():
                tStart = row['start']
                tStop = row['stop']
                heightRange = [yVals[userNames.index(row[dim1])]-0.5, yVals[userNames.index(row[dim1])]+0.5]
                start, end = heightRange
                step = (end - start) / len(gsNames)

                intervals = [[start + i * step, start + (i + 1) * step] for i in range(len(gsNames))]
                #print(intervals)
                userRows = df[(df[dim2] == gsName) & (df[dim1] == row[dim1])]
                userRows_tot = df[df[dim1] == row[dim1]]

                #h = create_patch(tStart, tStop, i + (userRows.index[0] - 1) / len(userRows),
                #                 i + userRows.index[0] / len(userRows), userRows_tot.index[0], len(gsNames))
                h = create_patch(tStart, tStop, intervals[gsNames.index(row[dim2])][0], intervals[gsNames.index(row[dim2])][1], i, len(gsNames))
                ax.add_patch(h)

    ax.set_xlabel('Epoch Days')
    ax.set_ylabel('User')
    ax.set_title('Station Schedule')

    ax.set_yticks(yVals)
    ax.set_yticklabels(userNames)

    #for i in range(len(userNames)):
    #    ax.plot([0, np.max(df['stop'])], [i, i], 'k')
        #ax.plot([0, 1], [i, i], 'k')

    plt.xlim([int(sys.argv[4]),int(sys.argv[5])])
    plt.ylim([0.4,len(userNames)+0.6])
    plt.legend(gsNames, loc='upper right')
    if sys.argv[7] =='1':
        plt.show()
    plt.savefig(sys.argv[6])


plotschedule(sys.argv[1])
