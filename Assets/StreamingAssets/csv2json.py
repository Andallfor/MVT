import sys
import csv
import json
from datetime import datetime

windowsDict = {"epochTime":"12-Dec-2025", "fileGenDate": datetime.now().strftime('%m-%d_%H%M')
, "windows":[]}
csvreader = csv.reader(open(sys.argv[1], 'r'))

for row in csvreader:
    source = row[0]
    destination = row[1]
    start = float(row[2])
    stop = float(row[3])
    contains_source_dest = any(d.get('source') == source and d.get('destination') == destination for d in windowsDict["windows"])
    if not contains_source_dest:
        windowsDict["windows"].append({"frequency": "Ka Band", "source": source, "destination": destination, "rate":0.02, "windows":[]})
    for dictionary in windowsDict["windows"]:
        if dictionary.get('source') == source and dictionary.get('destination') == destination:
            dictionary['windows'].append([start, stop])

print(windowsDict)
filePath = sys.argv[2]
with open(filePath, 'w') as json_file:
    json.dump(windowsDict, json_file)
