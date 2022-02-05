import pyxdf
import sys
import os
import numpy as np

filename = sys.argv[1]
data,header = pyxdf.load_xdf(filename)
# Check each data stream 
#   if nominal rate not set to 0
#       longest gaps and percent missing overall
#   if nominal reate set to 0 (or only lab remote?)
#      raw number of fired events
head, tail = os.path.split(filename)
print("Capture Summary")
print("Trial: " + tail)
print("")
for idx, stream in enumerate(data):
    # Will need to handle multichannel streams (pupil eyes).
    # Unfortunately, pupil labs apparently doesnt set a nominal srate
    #if float(stream['info']['nominal_srate'][0]) > 0:
    if stream['info']['name'][0] != "LabRemote":
        # find gaps and percent missing
        print(stream['info']['name'][0])
        if "Pupil Primitive" in stream['info']['name'][0]:
            # Show confidence
            percentLowCon = np.count_nonzero(stream['time_series'][:,1] == 0)/int(stream['footer']['info']['sample_count'][0]) *100
            print("Zero Confidence samples: %d%%" % percentLowCon)
        print("Max gap length (seconds): %.4f" % max(np.diff(np.array(stream['time_stamps']))))
        if float(stream['info']['nominal_srate'][0]) == 0:
            sRate = 1/np.mean(np.diff(np.array(stream['time_stamps'])))
        else:
            sRate = float(stream['info']['nominal_srate'][0])

        expectedSampNum = round((float(stream['footer']['info']['last_timestamp'][0]) - float(stream['footer']['info']['first_timestamp'][0])) * sRate)
        percentMissing = (int(stream['footer']['info']['sample_count'][0]) - expectedSampNum)/expectedSampNum * 100
        print("Missing samples: %d%%" % max(percentMissing,0))

    if stream['info']['name'][0] == "LabRemote":
        print(stream['info']['name'][0])
        print("Total Trigger Events: %d" % np.count_nonzero(stream['time_series'] == 2))
        print("Total Beep Events: %d" % np.count_nonzero(stream['time_series'] == 1))
    print("")
print("End")


sys.stdout.flush()