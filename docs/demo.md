# Introduction

* Michael Hedgpeth - as myself, not an NCR employee - this is my lunch hour
* Summit opportunity for improving windows

# Problems in Windows

* Windows added on after the fact, with a linux mindset that creates friction
* Chef running Ruby updating Chef is not natural in a Windows environment
* Logging not natural for windows-oriented operations - no rolling, no configuration, no specialized logging
* Scheduling Chef is difficult - scheduled task OK, but no standard way of managing it
* Pausing chef not standard and difficult
* Upgrading Chef difficult as well (chef can't upgrade itself, chef should not be running)
* Bootstrapping Chef when winrm isn't turned on is quite difficult as well

# Let's do a walkthrough

## Starting point

* Local VM running Windows 2016
* Zip file of cafe build
* Chef Server account (manage.chef.io), client.rb, and validator for bootstrapping

## Bootstrapping

### Install Cafe on the node
  - Copy files to a folder
  - No ruby, no .NET Framework, **no prerequisites**, can be targeted to any of [these targets](https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog#using-rids).
  - Run `cafe init` and reboot to add to PATH variable
  - Run `cafe service register` which adds cafe to services
  - Run `cafe service start` which starts the Server
  - Run `cafe status` to see output
  - View `http://localhost:59320`
  - Review architecture diagram to show roles

### Control your node remotely
  - update `client.json` on another machine to point at your node over port `59320`

### Install chef-client on the node 
  - Run `cafe chef version` and notice that it is not installed
  - Run `cafe chef download 12.16.42` - places it in the staging folder (this could be done by a chef cookbook as well)
  - Run `cafe chef install 12.16.42`
  - Run `cafe chef version` and notice that it is installed, show add/remove programs

### Bootstrap the node
  - Run `cafe chef bootstrap policy: webserver group: uat config: C:\users\mhedg\.chef\client.rb validator: C:\users\mhedg\validator.pem`
  - Run `cafe chef run`
    - view the chef-specific run on the Server

## Logging

* Chef logging to its own file, rolled
* NLog controlling the level, pattern, etc.
* Rest of server that is consumable by a log4X log viewer, like Log4View
* Complete history of what happened:
  - Run `cafe chef run`
  - Now let's restart the server with `cafe service restart`
  - Run `cafe chef status` to see that the history of chef runs is still there with valuable statistics of what's happening

## Scheduling

* Schedule Chef to run every X minutes, defined in `server.json`
* Easily view the schedule through the `cafe status` command
* Everything scheduled, including the ad-hoc things, are run **serially**. That means no conflicts!

## Pausing

People are going to pause Chef. Why not give them an easy way to do it so you can track it?

* Pause chef with `cafe chef pause`
* Run `cafe chef run`
* Run `cafe status`. Notice that the task is queued but not run yet since it is paused.
* Go to `http://localhost:59320` and see that it is paused
* Run `cafe chef resume` and see that chef starts running

## Some other ideas

### Configuration

Through a chef cookbook, how else would we do it?

### Change Cafe configuration through Chef
* Change the port cafe should listen on to port 80
* Update policy and push to chef server
* Run chef
* Show that the port change was scheduled to not interrupt anything
* Go to `http://localhost` and see cafe running

### Upgrade Chef Client in Chef
* Change the chef client version to `12.17.44`
* Update policy and push to chef server
* Run chef
* Show that the upgrade was scheduled after chef and proceeded just fine

# Conclusion

* What do you like?
* What did I miss?
* What would it take for you to use this?