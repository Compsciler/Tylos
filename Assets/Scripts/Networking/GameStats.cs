using System.Collections.Generic;

public class GameStats {
    Dictionary<int, int> unitsCreated = new Dictionary<int,int>();
    Dictionary<int, int> unitsDestroyed = new Dictionary<int,int>();
    Dictionary<int, int> basesCreated = new Dictionary<int,int>();
    Dictionary<int, int> basesDestroyed = new Dictionary<int,int>();

    public void AddUnitsCreated(int client_id, int numUnits){
        int created = unitsCreated.GetValueOrDefault(client_id) + numUnits;
        unitsCreated[client_id] = created;
    }

    public int GetUnitsCreated(int client_id){
        return unitsCreated.GetValueOrDefault(client_id);
    }

    public void AddUnitsDestroyed(int client_id, int numUnits){
        int destroyed = unitsDestroyed.GetValueOrDefault(client_id) + numUnits;
        unitsDestroyed[client_id] = destroyed;
    }

    public int GetUnitsDestroyed(int client_id){
        return unitsDestroyed.GetValueOrDefault(client_id);
    }

    public void AddBaseCreated(int client_id){
        int created = basesCreated.GetValueOrDefault(client_id) + 1;
        basesCreated[client_id] = created;
    }

    public int GetBasesCreated(int client_id){
        return basesCreated.GetValueOrDefault(client_id);
    }

    public void AddBaseDestroyed(int client_id){
        int destroyed = basesDestroyed.GetValueOrDefault(client_id) + 1;
        basesDestroyed[client_id] = destroyed;
    }
    public int GetBasesDestroyed(int client_id){
        return basesDestroyed.GetValueOrDefault(client_id);
    }
}