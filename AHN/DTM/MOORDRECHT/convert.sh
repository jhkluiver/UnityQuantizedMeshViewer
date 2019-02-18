#!/bin/bash
for file in $(ls *.tif); do 
    gdalwarp -t_srs EPSG:3857 $file mercator_$file; 
done