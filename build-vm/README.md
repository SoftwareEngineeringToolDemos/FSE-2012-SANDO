## Boot up your own Virtual Machine using vagrant

### Here's how you can create a Virtual Machine for SANDO :

1. Install [vagrant] (https://www.vagrantup.com/downloads.html) and [virtualbox] (https://www.virtualbox.org/wiki/Downloads) (preferrably latest versions) on host machine.
2. Download the [Vagrantfile] (Vagrantfile) and the [scripts] (https://github.com/SoftwareEngineeringToolDemos/FSE-2012-SANDO/tree/master/build-vm/scripts) from [build-vm] () folder to the folder on your machine where you want to install the VM.
3. Navigate to that folder (via bash on Linux or Command Prompt on Windows) and execute the command :  
      "vagrant up --provider virtualbox"

### Note :  
 -  The Virtual Machine will take a long time as it installs windows, wait for the "vagrant up" command to complete as it provisions the VM for use.
 -  Deploys Base Vagrant Box : [modernIE/w7-ie11] (https://atlas.hashicorp.com/modernIE/boxes/w7-ie11)
 -  Default VM Login Credentials:  
      user: vagrant  
      password: vagrant
 -  The license of the windows is updated by the [owner](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11) of the box
 
 
### Acknowledgements :
 - The box used for this virtual machine is provided by [modernIE/w7-ie11](https://atlas.hashicorp.com/modernIE/boxes/w7-ie11)
