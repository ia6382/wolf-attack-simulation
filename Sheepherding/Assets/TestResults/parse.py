import numpy as np
from matplotlib import pyplot as plt

def parse(data):
    firstKillTimes = []
    killTimes = []

    data = data.split("\n---\n")
    header = data[0]
    numOfSimulations = len(data[1:])

    for i in data[1:]:
        f = i.split("\n")[0]
        if f == "---": #ce v simulaciji ni prislo do uboja
            continue
        firstKillTimes.append(float(f))
        for j in i.split("\n"):
            if j != "":
                killTimes.append(float(j))

    avgKills = len(killTimes)/numOfSimulations
    print("povprečno število ubitih ovc: "+str(avgKills))

    return firstKillTimes



def plotTactics():
    #read files
    f = open("closest.txt", "r")
    data1 = f.read()
    f.close
    f = open("isolated.txt", "r")
    data2 = f.read()
    f.close
    f = open("strombom.txt", "r")
    data3 = f.read()
    f.close
    f = open("boids.txt", "r")
    data4 = f.read()
    f.close  

    #parse data
    firstKillTimes1 = parse(data1)
    firstKillTimes2 = parse(data2)
    firstKillTimes3 = parse(data3)
    firstKillTimes4 = parse(data4)
    """
    print("***ČAS PRVEGA UBOJA***")
    sd = np.std(firstKillTimes)
    print("standardni odklon: "+str(sd))
    md = np.median(firstKillTimes)
    print("mediana vrednost: "+str(md))
    avg = np.average(firstKillTimes)
    print("povprečna vrednost: "+str(avg))
    """

    #plot histograms in subplot
    fig, (h1, h2, h3, h4) = plt.subplots(1, 4, sharex=True, sharey=True)
    fig.suptitle("Taktike plenjenja volkov", fontsize=16)
    fig.text(0.5, 0.01, 'čas do prvega ulova [s]', ha='center')
    fig.text(0.07, 0.5, 'število simulacij', va='center', rotation='vertical')

    h1.hist(firstKillTimes1, density=False, bins=5)
    h1.set_title("Napad na najbližjo ovco")

    h2.hist(firstKillTimes2, density=False, bins=5)
    h2.set_title("Napad na najbolj izolirano ovco")

    h3.hist(firstKillTimes3, density=False, bins=5)
    h3.set_title("Napad z obkoljevanjem")

    h4.hist(firstKillTimes4, density=False, bins=5, color="red")
    h4.set_title("Napad po modelu Boids")

    plt.show()

def plotDogs():
    #read files
    f = open("1dogE.txt", "r")
    data1 = f.read()
    f.close
    f = open("2dogE.txt", "r")
    data2 = f.read()
    f.close
    f = open("3dogE.txt", "r")
    data3 = f.read()
    f.close
    f = open("4dogEAT.txt", "r")
    data4 = f.read()
    f.close  

    #parse data
    firstKillTimes1 = parse(data1)
    firstKillTimes2 = parse(data2)
    firstKillTimes3 = parse(data3)
    firstKillTimes4 = parse(data4)
    """
    print("***ČAS PRVEGA UBOJA***")
    sd = np.std(firstKillTimes)
    print("standardni odklon: "+str(sd))
    md = np.median(firstKillTimes)
    print("mediana vrednost: "+str(md))
    avg = np.average(firstKillTimes)
    print("povprečna vrednost: "+str(avg))
    """

    #plot histograms in subplot
    fig, (h1, h2, h3, h4) = plt.subplots(1, 4, sharex=True, sharey=True)
    fig.suptitle("Branjenje črede", fontsize=16)
    fig.text(0.5, 0.01, 'čas do prvega ulova [s]', ha='center')
    fig.text(0.07, 0.5, 'število simulacij', va='center', rotation='vertical')

    h1.hist(firstKillTimes1, density=False, bins=5)
    h1.set_title("1 pes čuvaj")

    h2.hist(firstKillTimes2, density=False, bins=5)
    h2.set_title("2 psa čuvaja")

    h3.hist(firstKillTimes3, density=False, bins=5)
    h3.set_title("3 psi čuvaji")

    h4.hist(firstKillTimes4, density=False, bins=5)
    h4.set_title("4 psi čuvaji")

    plt.show()

if __name__ == "__main__":
    plotDogs()