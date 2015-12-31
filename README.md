# Wrecking Ball
### Destroy stuff in the server to clear lag

This addon allows you clear stuff in a defined radius using filters


## Available Commands
Command | Action
------- | -------
/wreck [filter] [radius]				| Creates new items destruction list
/wreck confirm							| Confirms list destruction
/wreck abort							| Aborts list destruction
/wreck scan [filter] [radius]			| Scan for [filter] in [radius] and list
/wreck teleport [b/s]					| Teleports caller to the next [b] (barricade) [s] (structure) +200m away


## Available Filters
Filter | Element
------- | -------
b				| Bed
t				| Trap
d				| Door
c				| Container
l				| Ladder
w				| Wall / Window
p				| Pillar
r				| Roof / Hole
s				| Stair / Ramp
m				| Freeform Buildables
n				| Signs
g				| Guards (Barricades / Fortifications)
i				| Illumination / Generators / Fireplaces
a				| Agriculture (plantations / platers)
v				| Vehicles
*				| Everything except Zombies
z				| Zombies (killing too many zombies at once, crashes the server)
!				| Uncategorized Elements (Elements that don't have an id associated with it in the plugin like mod structures and barricaeds.)


## Available Permissions
Permission | Action
------- | -------
wreck				| allow caller to wreck stuff


## Other Options
Option | Action
------- | -------
Enabled								| Enables and disables the addon (does not apply to admins)


## Todo List:
* Randomize the location selection of the teleport feature.
* Export the Element Id lists to the config file to make it so that custom Id's can be added to the plugin without the need to modify and build a custom plugin with the added Id's.
