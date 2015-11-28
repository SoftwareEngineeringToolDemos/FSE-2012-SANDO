## Boot up your own Virtual Machine

### Here's how you can create a Virtual Machine for SANDO ...

#### Steps:
1. Install [vagrant] (https://www.vagrantup.com/downloads.html) and [virtualbox] (https://www.virtualbox.org/wiki/Downloads) (preferrably latest versions) on host machine.
2. Download the contents of [build-vm] () folder to the folder on your machine where you want to install the VM (can use git clone).
3. Navigate to that folder (via bash on Linux or Command Prompt on Windows) and execute the command :  
    ```
    $ vagrant up --provider virtualbox
    ```

#### Note :  
 -  The Virtual Machine will take a long time as it installs windows, wait for the "vagrant up" command to complete as it provisions the VM for use.
 -  Deploys Base Vagrant Box : [modernIE/w7-ie11] (https://atlas.hashicorp.com/modernIE/boxes/w7-ie11)
 -  Default VM Login Credentials:  
      user: `vagrant`  
      password: `vagrant`
 -  The license of the windows is updated by the [owner](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11) of the box
 -  The visual Studio which starts at startup is an evaluation version provided by [Microsoft](https://www.microsoft.com/en-US/Download/details.aspx?id=30654)
 -  Visual Studio will ask for few things at startup
      1. Dialogue box saying: This program has known compatibility issues (click `Run Program`)
      2. Asks for license (click `Cancel`)
      3. Asks for development environment (click `C# Development Settings`(on the left box) and then click `Start Visual Studio`)
      4. Repeat Step 1 if required
      5. Go to `Tools` -> `Extensions and Updates`
      6. Click on `Sando Code Search Tool` and enable it if it is disabled
      7. Restart MS Visual Studio
      8. You are god to go, refer to Readme.txt on Desktop for any doubts/steps/help
 
 
#### Acknowledgements :
 - The box used for this virtual machine is provided by [modernIE/w7-ie11](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11)
 - The commands to install dot-net-4.5 have been taken from [chocolatey](https://chocolatey.org/) website.
