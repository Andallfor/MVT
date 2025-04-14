import matplotlib.pyplot as plt
import numpy as np

with open('benchmarking.txt') as f:
    lines = f.readlines()
    
    nPoints = {}
    std = {}
    
    avg = 0
    avgN = 0
    
    px = []
    py = []
    
    sx = []
    sy = []
    
    for line in lines:
        header, value = line.split(': ')
        
        if 'npoints_' in header:
            name = header.split('_')[-1]
            nPoints[name] = int(value)
        
        if '_std' in header and 'ele_' in header:
            name = header.split('_')[1]
            std[name] = float(value)
        
        if 'file_size_bytes_' in header:
            name = header.split('_')[-1]
            size = int(value)
            
            sx.append(std[name])
            sy.append(size / nPoints[name])
        
        if 'speed_mesh_' in header:
            name = header.split('_')[-1]
            
            if name not in nPoints.keys():
                continue
            
            avgN += 1
            value = value[1:-4]
            t0, t1, t2, t3, t4, t5 = value.split(', ')
            t0, t1, t2, t3, t4, t5 = int(t0), int(t1), int(t2), int(t3), int(t4), int(t5)

            n = nPoints[name]
            points = [n, n / (2 * 2), n / (4 * 4), n / (8 * 8), n / (16 * 16), n / (32 * 32)]
            #speed = [t0, t1, t2, t3, t4, t5]
            speed = [points[0] / t0, points[1] / t1, points[2] / t2, points[3] / t3, points[4] / t4, points[5] / t5]
            avg += speed[0]
            
            print(n)
            
            #print(name, speed)
            
            px += points
            py += speed

    print(avg / avgN)
    
    print(px)
    print(py)

    plt.xscale('log')
    plt.scatter(px, py)
    plt.xlabel('Dataset size (log)')
    plt.ylabel('Points per ms')

    #plt.scatter(sx, sy)
    #print(np.min(sy))
    #print(np.max(sy))
    #plt.ylabel('Bytes per point')
    #plt.xlabel('Standard Deviation')

    plt.show()
