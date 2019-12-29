<h1> AI Dungeon PathFinding </h1>
<p class="mb-5">
  The simulation offers 3 unique types of AI; Procedural dungeon generation, path node 
  generation, and character state machines. 
  <br/><br/>
  <span style="color:rgb(255, 85, 85)"><b>
    There is a build for android VR see below for details.
  </b></span> 
</p>
<h2 class="text-secondary text-uppercase mb-0">Dungeon Generation</h4>
<br/>
<p class="mb-5">
  The dungeon generation works in 6 steps which are: building the critical path, building rooms, 
  expanding rooms, creating doors, joining rooms, and building the dungeon. Each of these steps 
  are laid out in the AIDungeonDiagram.jpg file.
</p>
<h2 class="text-secondary text-uppercase mb-0">Path Nodes</h4>
<br/>
<p class="mb-5">
  Path Node Generation works by first placing path nodes on each dungeon floor tile. The generator
  then creates connections based on one node next to another. Connections that would connect if
  there was no door in between the two would create a closed door connection. This connection is
  then paired with the door to allow for it to open and close. Each connection can be traveled 
  across by the AI characters.
</p>
<h2 class="text-secondary text-uppercase mb-0">State Machine</h4>
<br/>
<p class="mb-5">
  The state machine for this project is fairly basic, but it demonstrates the important aspect
  of having a character respond to external stimuli. The player flees whenever encountering an
  enemy. The enemies chase when encountering a player. When the characters are not fleeing or
  following, they chose a random pathnode and attempt to travel to it.
  <br/><br/>
  <b>
  You can find a build of the project in the releases section.
  If you have an Android device with a VR headset or google cardboard, there is an APK build 
  that supports it also under the releases page.
  </b>
</p>
