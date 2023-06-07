class Airlock{
    private List<IMyAirVent> AirVents { get; }
    private List<IMyDoor> InteriorDoors { get; }
    private List<IMyDoor> ExteriorDoors { get; }

    public Airlock(){
        this.AirVents = new List<IMyAirVent>();
        this.InteriorDoors = new List<IMyDoor>();
        this.ExteriorDoors = new List<IMyDoor>();
    }

    public void AddAirVent(IMyAirVent vent){
        this.AirVents.Add(vent);
    }

    public void AddInteriorDoor(IMyDoor door){
        this.InteriorDoors.Add(door);
    }

    public void AddExteriorDoor(IMyDoor door){
        this.ExteriorDoors.Add(door);
    }

    private bool IsInteriorOpen(){
        for (int i = 0; i < this.InteriorDoors.Count; i++){
            if (this.InteriorDoors[i].Status != DoorStatus.Closed){
                return true;
            }
        }
        return false;
    }

    private bool IsExteriorOpen(){
        for (int i = 0; i < this.ExteriorDoors.Count; i++){
            if (this.ExteriorDoors[i].Status != DoorStatus.Closed){
                return true;
            }
        }
        return false;
    }

    private void LockInteriorDoors(){
        for (int i = 0; i < this.InteriorDoors.Count; i++){
            this.InteriorDoors[i].ApplyAction("OnOff_Off");
        }
    }

    private void LockExteriorDoors(){
        for (int i = 0; i < this.ExteriorDoors.Count; i++){
            this.ExteriorDoors[i].ApplyAction("OnOff_Off");
        }
    }

    private void UnlockDoors(){
        for (int i = 0; i < this.InteriorDoors.Count; i++){
            this.InteriorDoors[i].ApplyAction("OnOff_On");
        }
        for (int i = 0; i < this.ExteriorDoors.Count; i++){
            this.ExteriorDoors[i].ApplyAction("OnOff_On");
        }
    }

    private void DisableAirVents(){
        for (int i = 0; i < this.AirVents.Count; i++){
            this.AirVents[i].ApplyAction("OnOff_Off");
        }
    }

    private void EnableAirVents(){
        for (int i = 0; i < this.AirVents.Count; i++){
            this.AirVents[i].ApplyAction("OnOff_On");
        }
    }

    public void Update(){
        if (this.IsInteriorOpen()){
            this.LockExteriorDoors();
            this.DisableAirVents();
        }
        else if (this.IsExteriorOpen()){
            this.LockInteriorDoors();
            this.DisableAirVents();
        }
        else{
            this.UnlockDoors();
            this.EnableAirVents();
        }
    }
}

const UpdateFrequency DEFAULT_UPDATE_FREQUENCY = UpdateFrequency.Update10;
const string AIRLOCK_TAG = "[Airlock]";
const string AIRLOCK_INTERIOR_DOOR_TAG = "[I]";
const string AIRLOCK_EXTERIOR_DOOR_TAG = "[E]";
const char TAG_CHAR_START = '[';
const char TAG_CHAR_END = ']';

List<Airlock> airlocks;

public Program(){
    ClearDebug();
    Runtime.UpdateFrequency = DEFAULT_UPDATE_FREQUENCY;
    airlocks = new List<Airlock>();

    SetupAirlocks();
}

public void Main(string argument, UpdateType updateSource){
    for (int i = 0; i < airlocks.Count; i++){
        try{
            airlocks[i].Update();
        }
        catch(Exception e){
            PrintDebug("Error with airlock " + i);
        }
    }
}

void SetupAirlocks(){
    List<IMyTerminalBlock> blocks = GetAirlockTaggedBlocks();
    List<string> groups = GetGroupNames(blocks);

    for (int i = 0; i < groups.Count; i++){
        string groupName = groups[i];
        Airlock airlock = new Airlock();
        airlocks.Add(airlock);
        PrintDebug(String.Format("Creating airlock group '{0}'", groupName));
        
        for (int j = 0; j < blocks.Count; j++){
            IMyTerminalBlock block = blocks[j];
            string blockName = block.CustomName;
            List<string> tags = GetTagsFromBlock(block);

            if (tags.Contains(groupName)){
                if (IsAirVent(block)){
                    airlock.AddAirVent((IMyAirVent) block);
                    PrintDebug(String.Format("Found vent '{0}' for airlock '{1}'", blockName, groupName));
                }
                else if (IsDoor(block)){
                    if (tags.Contains(AIRLOCK_INTERIOR_DOOR_TAG)){
                        airlock.AddInteriorDoor((IMyDoor) block);
                        PrintDebug(String.Format("Found interior door '{0}' for airlock '{1}'", blockName, groupName));
                    }
                    else if (tags.Contains(AIRLOCK_EXTERIOR_DOOR_TAG)){
                        airlock.AddExteriorDoor((IMyDoor) block);
                        PrintDebug(String.Format("Found exterior door '{0}' for airlock '{1}'", blockName, groupName));
                    }
                    else{
                        airlock.AddInteriorDoor((IMyDoor) block);
                        PrintDebug(String.Format("Found unknown door, assuming interior '{0}' for airlock '{1}'", blockName, groupName));
                    }
                }
                else{
                    PrintDebug(String.Format("Found unknown block '{0}' for airlock '{1}'", blockName, groupName));
                }
            }
        }
    }
}

List<string> GetGroupNames(List<IMyTerminalBlock> blocks){
    List<string> names = new List<string>();

    for (int i = 0; i < blocks.Count; i++){
        IMyTerminalBlock block = blocks[i];
        List<string> tags = GetTagsFromBlock(block);
        string name = GetGroupTagNameOfBlock(block, tags);

        if (name != null){
            if (!names.Contains(name)){
                names.Add(name);
            }
        }
    }

    return names;
}

string GetGroupTagNameOfBlock(IMyTerminalBlock block, List<string> tags){
    for (int i = 0; i < tags.Count; i++){
        string tag = tags[i];
        if (tag != AIRLOCK_TAG && tag != AIRLOCK_INTERIOR_DOOR_TAG && tag != AIRLOCK_EXTERIOR_DOOR_TAG){
            return tag;
        }
    }

    return null;
}

bool IsAirVent(IMyTerminalBlock block){
    try{
        IMyAirVent vent = (IMyAirVent) block;
        return true;
    }
    catch(Exception e){
        return false;
    }
}

bool IsDoor(IMyTerminalBlock block){
    try{
        IMyDoor door = (IMyDoor) block;
        return true;
    }
    catch(Exception e){
        return false;
    }
}

List<string> GetTagsFromBlock(IMyTerminalBlock block){
    List<string> tags = new List<string>();
    bool isTag = false;
    string tag = "";

    for (int i = 0; i < block.CustomName.Length; i++){
        char c = block.CustomName[i];
        switch(c){
            case TAG_CHAR_START:
                isTag = true;
                tag = "";
                break;
            case TAG_CHAR_END:
                isTag = false;
                tags.Add(TAG_CHAR_START + tag + TAG_CHAR_END);
                break;
            default:
                if (isTag){
                    tag += c;
                }
                break;
        }
    }

    return tags;
}

List<IMyTerminalBlock> GetAirlockTaggedBlocks(){
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(AIRLOCK_TAG, blocks);
    return blocks;
}

void ClearDebug(){
    Me.CustomData = "";
}

void PrintDebug(string data){
    Me.CustomData = Me.CustomData + data + '\n';
}