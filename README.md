# Investigating Data-Oriented Design
This repo contains the thesis and source code for my master project, Investigating Data-Oriented Design, at NTNU Gjøvik. 

## Thesis
The thesis itself can be found in this [repository](https://github.com/Per-Morten/master_project/blob/master/investigating_dod_thesis.pdf) (I recommend downloading it rather than using Githubs PDF reader as it, in my case, doesn't load the entire thesis). Alternatively you can find the thesis at [NTNUOpen]([https://hdl.handle.net/11250/2677763](https://ntnuopen.ntnu.no/ntnu-xmlui/handle/11250/2677763)). 

The thesis essentially me trying to understand DOD through literature analysis, in-depth interviews with industry practitioners of DOD, and a practical application of my understanding of DOD, with feedback from some of the practitioners.

## bibtex citation:
```
@mastersthesis{investigating_dod_pm_straume,
    author = {Straume, {Per-Morten}},
    title = {Investigating Data-Oriented Design},
    school = {Norwegian University of Science and Technology, Gj{\o}vik},
    year = {2019},
    publisher = {NTNU},
    note = {Available from: \url{https://hdl.handle.net/11250/2677763}}
}
```

## Practical Application - Streaming IFC Models from disk
A part of this thesis was to stream IFC models from disk, the source code for this is also located at this repository.

### Video Demonstrations
A video demonstrating walking around in the WestRiverSide Hospital can be seen at: [https://www.youtube.com/watch?v=jJToc9uKA00](https://www.youtube.com/watch?v=jJToc9uKA00)

A video demonstrating what is streamed in and out can be seen at:
[https://www.youtube.com/watch?v=gEE3nMjxk_Y](https://www.youtube.com/watch?v=gEE3nMjxk_Y)

### Libraries & Usage terms
Various libraries and resources were used for this project. Those that still remain in the repo are listed below. Obviously, Unity was used throughout the project (personal license)

|Library        | Author | Terms                                                    | URL   |
|---------------|--------|----------------------------------------------------------|-------|
|IFCEngine      | RDF Ltd. | Free for academic/non-commercial use with attribution  |http://www.ifcbrowser.com/|
|NoAllocHelpers | Timothy Raines | MIT License                                      |https://forum.unity.com/threads/nativearray-and-mesh.522951/#post-3842671       |
|Singleton Base | Angry Ant | CC BY-SA 3.0 License | http://wiki.unity3d.com/index.php/Singleton |
|Graphy | Tayx | MIT License | https://assetstore.unity.com/packages/tools/gui/graphy-ultimate-fps-counter-stats-monitor-debugger-105778 & https://github.com/Tayx94/graphy
|SteamVR | Valve Software | BSD 3-Clause License | https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647

### WestRiverSide Hospital
Licensed under cc-by-sa-3.0, provided by Autodesk Inc. for research.
Found at: http://openifcmodel.cs.auckland.ac.nz/Model/Details/305

### Downloading and running
To run the application, clone this repo and set up a Unity project around it (tested with 2019.2.0f).
To convert a scene open and run the ConverterScene. While running, find the Scripts gameobject, click the gear on the Ifc Converter Script component and choose Convert Model. Converting the model can take a while.
When you see the message: `Meshes Converted, Can now write to file`, press the same gear, and choose Write to file.
When you see the message: `Finished writing to file` you can stop the application, and switch to the ECSStreamingScene (if you don't have VR)
or the ECSVRScene if you want to use VR. This application has only been tested with Windows Mixed Reality goggles, so I don't think there are keybindings for the other input methods.

#### Converting other models
Only the WestRiverSide Hospital model is provided, in the case you want to convert another model, simply store it in a folder within the IFCFiles folder.
Change the serialized foldername value in the Ifc Converter Script Component in the ConverterScene to the nsme of the folder where you put your .ifc files.
It might also be an idea to remove the line: `IfcEngine.x64.setVertexOffset(mModel, -10021.29, -125633.9, -166011.2);` As that was an attempt at trying do deal with floating point issues in the Westside model (which didn't seem to work). You can either delete the line, or try to change the offset to something that fits your model.
