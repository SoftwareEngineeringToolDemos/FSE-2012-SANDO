## Boot up your own Virtual Machine

### Here's how you can create a Virtual Machine for SANDO ...

#### Steps:
1. Install [vagrant] (https://www.vagrantup.com/downloads.html) and [virtualbox] (https://www.virtualbox.org/wiki/Downloads) (preferrably latest versions) on host machine.
2. Download the contents of [build-vm] () folder to the folder on your machine where you want to install the VM (can use git clone).
3. Navigate to that folder (via bash on Linux or Command Prompt on Windows) and execute the command:  
    ```
    $ vagrant up --provider virtualbox
    ```
4. Virtual Machine may reload once or twice while provisioning.
5. After provisioning finishes, Visual Studio will start after reload. Visual Studio is an evaluation version and hence will ask for few things at startup:
    1. Dialogue box saying: This program has known compatibility issues (click checkbox next to `Don't show me this message again` and choose `Run Program`)
    2. Asks for license (click `Cancel`)
    3. Wait for Visual Studio to load settings.
    5. Go to `Tools` -> `Extensions and Updates`
    6. Click on `Sando Code Search Tool` and enable it if it is disabled (if Sando Code Search Tool is not present, highly unlikely, install the UI.vsix file in the folder Sando (on desktop))
    7. MS Visual Studio will ask for restart. Click `Restart`.
    8. You are good to go, refer to Readme.txt on Desktop for any doubts/steps/help

#### Note:  
 -  The Virtual Machine will take a long time as it installs windows, wait for the "vagrant up" command to complete as it provisions the VM for use.
 -  Default VM Login Credentials (if asked):  
      username: `IEUser`  
      password: `Passw0rd!`
 -  The license of the windows is updated by the [owner](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11) of the box
 -  The visual Studio which starts at startup is an evaluation version provided by [Microsoft](https://www.microsoft.com/en-US/Download/details.aspx?id=30654)
 
#### Acknowledgements:
 - The box used for this virtual machine is provided by [modernIE/w7-ie11](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11)
 - Uses [Info-ZIP] (http://www.info-zip.org/UnZip.html#Downloads) unzip software
 
