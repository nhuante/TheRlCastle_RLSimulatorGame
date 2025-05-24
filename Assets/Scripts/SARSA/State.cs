using UnityEngine; 

// represents a state in a state:action pair 
public struct State {
    public Vector2Int enemy_pos; 
    public Vector2Int player_pos;

    public State(Vector2Int enemyPos, Vector2Int player_pos) {
        this.enemy_pos = enemyPos;
        this.player_pos = player_pos;
    }

    public override int GetHashCode() => enemy_pos.GetHashCode() ^ player_pos.GetHashCode();

    // used when comparing equality between
    public override bool Equals(object obj) {
        // if other object is not a state type, they can't be equal 
        if (!(obj is State)) return false;
        // otherwise, check if both the enemy and player pos are equal 
        State other = (State)obj;
        return enemy_pos == other.enemy_pos && player_pos == other.player_pos;

    }

    public override string ToString() {
        return $"Enemy: {enemy_pos}, Player: {player_pos}";
    }
}