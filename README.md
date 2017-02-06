# HoloLens 3D Mapping in Unity
See http://peted.azurewebsites.net/hololens-3d-mapping/ for further details and usage
[![Alt text](https://img.youtube.com/vi/FSyBHbckXew/0.jpg)](https://www.youtube.com/watch?v=FSyBHbckXew)
![alt tag](https://raw.github.com/peted70/geojsontomesh/master/img/somerset%20house.PNG)
![alt tag](https://raw.github.com/peted70/geojsontomesh/master/img/londoninunity.PNG)
# Editor
To use you can add an empty GameObject into your scene and then add the 
ThreeDMapScript as a new component to that GameObject. The 
custom editor for this component will provide some inputs to allow you to define 
a bounding box in terms of latitude and longitude. Also, you can specify the 
height of the levels used for the buildings. This could also be sourced from 
other data sets so could be a more accurate representation of the building 
heights. Once set the Generate Map button will cause the script to 
call the REST API to retrieve the GeoJSON and the satellite image, generate the 
meshes and apply the required material. Each building is currently represented 
by a separate mesh as can be seen in the scene hierarchy window and is named 
from data in the GeoJSON. 
![alt tag](https://raw.github.com/peted70/geojsontomesh/master/img/custom-editor.PNG)
# REST API
To run the REST API either load the ASP.NET Core project in Visual Studio and press F5 or navigate in a shell to the folder containing the project.json file and execute the command dotnet run
