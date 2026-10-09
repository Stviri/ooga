namespace Ooga;

// The shape of an ooga program after reading it. Every piece remembers where it came from.

public abstract class Node
{
    public int Line;
    public int Col;
}

// ---------- values (expressions) ----------

public abstract class Expr : Node { }

public class LiteralExpr : Expr { public object Value; }
public class NameExpr : Expr { public string Name; }
public class MathExpr : Expr { public string Op; public Expr Left, Right; }
public class NegateExpr : Expr { public Expr Inner; }
public enum CompareOp { Same, Big, Small, BigOrSame, SmallOrSame }
public class CompareExpr : Expr { public CompareOp Op; public bool Not; public Expr Left, Right; }
public class LogicExpr : Expr { public string Op; public Expr Left, Right; } // "and" / "or"
public class NotExpr : Expr { public Expr Inner; }
public class AskExpr : Expr { public Expr Prompt; }                          // Prompt can be null
public class RandomExpr : Expr { public Expr Low, High; }
public class CallExpr : Expr { public string Name; public List<Expr> Args = new(); }

// ---------- lines (statements) ----------

public abstract class Stmt : Node { }

public class KindStmt : Stmt { public string Kind; }                         // me is player
public class HasStmt : Stmt { public string Name; public Expr Value; }        // me has health 100
public class SetStmt : Stmt { public string Name; public Expr Value; }        // health is 50
public class ChangeStmt : Stmt { public string Name; public bool Gain; public Expr Amount; } // health gain/lose 5
public class SayStmt : Stmt { public Expr Value; }
public class IfStmt : Stmt
{
    public List<(Expr Cond, List<Stmt> Body)> Branches = new();
    public List<Stmt> Else;                                                  // null when no else
}
public class RepeatStmt : Stmt { public Expr Times; public List<Stmt> Body; }
public class RepeatWhileStmt : Stmt { public Expr Cond; public List<Stmt> Body; }
public class CountStmt : Stmt { public string Name; public Expr From, To; public List<Stmt> Body; }
public class StopStmt : Stmt { }
public class SkipStmt : Stmt { }
public class GiveStmt : Stmt { public Expr Value; }                           // Value can be null
public class WaitStmt : Stmt { public Expr Seconds; }
public class DieStmt : Stmt { }
public class CallStmt : Stmt { public CallExpr Call; }
public class CanStmt : Stmt { public string Name; public List<string> Params = new(); public List<Stmt> Body; } // me can jump

public class OogaProgram : Node { public List<Stmt> Body = new(); }
