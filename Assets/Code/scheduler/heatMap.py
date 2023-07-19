import collections

import numpy as np
import matplotlib.pyplot as plt
import json
import seaborn as sns
import sys
from collections import OrderedDict

#plt.subplots_adjust(left=0, bottom=0.0,right=1,top=1)
plt.figure(figsize=(14, 7))
f = open(sys.argv[1])
print("opened")
jsonData = json.load(f)
jsonData = collections.OrderedDict(jsonData)
jsonData = OrderedDict(sorted(jsonData.items(), key=lambda x: x[1]['priority']))
print("loaded json")
yAxisLabels = [y for y in jsonData]
xAxisLabels = [int(x) for x in jsonData[yAxisLabels[0]]['boxes'] if int(x) >= int(sys.argv[3]) and int(x) < int(sys.argv[4])]
data = np.array([[float(jsonData[y]['boxes'][str(x)]) for x in xAxisLabels] for y in yAxisLabels])
print("got data")
heatmap = sns.heatmap(data, xticklabels = xAxisLabels, yticklabels = yAxisLabels, square=False, cmap="RdBu", center=0, annot=True, fmt='.2f',
                      annot_kws={
                          'fontsize':8
                      }, vmin=-1, vmax=1)
print("made heatmap")
#heatmap.set_xticklabels(heatmap.get_xmajorticklabels(), fontsize = 12)
#heatmap.set_yticklabels(heatmap.get_ymajorticklabels(), fontsize = 8)
#heatmap.set(xlabel='Box', ylabel="User")
heatmap.set_xlabel('Box',fontsize=12, fontweight='bold')
heatmap.set_ylabel('User',fontsize=12, fontweight='bold')
plt.title("How Long Each User Spends in Boxes")
print("made labels")
#plt.show()
plt.savefig(sys.argv[2], bbox_inches='tight', dpi = 300)
print("saved")
