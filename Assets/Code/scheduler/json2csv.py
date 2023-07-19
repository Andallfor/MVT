import json
import csv
import sys

with open(sys.argv[1]) as json_file:
    data = json.load(json_file)


data_file = open(sys.argv[2], 'w', newline='')
csv_writer = csv.writer(data_file)
count = 0

for dat in data:
    if count == 0:
        # Writing headers of CSV file
        header = dat.keys()
        csv_writer.writerow(header)
        count += 1

    # Writing data of CSV file
    csv_writer.writerow(dat.values())

data_file.close()