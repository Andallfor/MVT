import glymur
import numpy as np
import time

INPUT = "C:/Users/leozw/Downloads/ASF/data.asc"
OUTPUT_JP2 = "C:/Users/leozw/Downloads/ASF/data.jp2"
OUTPUT_METADATA = "C:/Users/leozw/Downloads/ASF/metadata.txt"

glymur.set_option('lib.num_threads', 8)

def printProgressBar (iteration, total, prefix = '', suffix = '', decimals = 1, length = 100, fill = '█', printEnd = "\r"):
    """
    Call in a loop to create terminal progress bar
    @params:
        iteration   - Required  : current iteration (Int)
        total       - Required  : total iterations (Int)
        prefix      - Optional  : prefix string (Str)
        suffix      - Optional  : suffix string (Str)
        decimals    - Optional  : positive number of decimals in percent complete (Int)
        length      - Optional  : character length of bar (Int)
        fill        - Optional  : bar fill character (Str)
        printEnd    - Optional  : end character (e.g. "\r", "\r\n") (Str)
    """
    percent = ("{0:." + str(decimals) + "f}").format(100 * (iteration / float(total)))
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {percent}% {suffix}', end = printEnd)
    # Print New Line on Complete
    if iteration == total: 
        print()

with open(INPUT, "r") as f:
    head: list[str] = [next(f) for _ in range(10)]
    
    # the asc files have variable data headers, so just consume way more than
    # they normally have and then filter
    metadata = {}
    for line in head:
        if line.strip()[0].isdigit():
            break
        
        [name, value] = line.split()
        metadata[name] = float(value) if float(value) != int(float(value)) else int(value) # shhh
    
    metadata["res"] = 10
    
    print(metadata)
    
    # now read the actual data
    dataArr = np.ndarray(shape=(metadata['nrows'], metadata['ncols']), dtype=np.uint16)

    f.seek(0)
    for i in range(len(metadata)):
        next(f)
    
    s1 = time.time()
    for row in range(metadata["nrows"]):
        line = next(f).split()
        for col, value in enumerate(line):
            v = int(float(value)) + 32767
            if v < 0:
                v = 0
            dataArr[row, col] = v
        if row % 200 == 0:
            eta =  ((time.time() - s1) / max(row / metadata["nrows"], 0.000001)) - (time.time() - s1)
            printProgressBar(row, metadata["nrows"], suffix=f"ETA: {int(eta)}s")

    glymur.Jp2k(OUTPUT_JP2, data=dataArr, cratios=[1024, 512, 256, 128, 64, 32, 16, 8, 4, 2, 1])
    
    s = ""
    for key, value in metadata.items():
        s += f"{key} {value}\n"
    with open(OUTPUT_METADATA, "w") as w:
        w.write(s)

print("Done")