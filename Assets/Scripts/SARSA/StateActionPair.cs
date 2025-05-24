using System;

// represents a state:action pair in the q-table


[Serializable]
public struct StateActionPair {
    // components 
    public State state;    
    public string action; 

    public StateActionPair(State s, string a) {
        this.state = s; 
        this.action = a;
    }

    public override int GetHashCode() => state.GetHashCode() ^ action.GetHashCode();

    // to check if 2 state:action pairs are equal 
    public override bool Equals(object obj) {
        // if not a state action pair, they can't be equal
        if (!(obj is StateActionPair)) return false; 
        // otherwise, check if both their states and their actions are equal 
        StateActionPair other = (StateActionPair)obj; // convert to a SA pair
        return state.Equals(other.state) && action == other.action;
    }
}