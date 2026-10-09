namespace Ooga;

// The shape of an ooga program after reading it. Every piece remembers where it came from.

public abstract class Node
{
    public int Line;
    public int Col;
    public string File;     // which script file this came from
}

// ---------- values (expressions) ----------

public abstract class Expr : Node { }

public class LiteralExpr : Expr { public object Value; }
public class NameExpr : Expr { public string Name; }
public class MathExpr : Expr { public string Op; public Expr Left, Right; }        // + - * / %
public class NegateExpr : Expr { public Expr Inner; }
public enum CompareOp { Same, Big, Small, BigOrSame, SmallOrSame }
public class CompareExpr : Expr { public CompareOp Op; public bool Not; public Expr Left, Right; }
public class HasExpr : Expr { public Expr Holder, Item; }                       // bag has 5
public class LogicExpr : Expr { public string Op; public Expr Left, Right; }   // "and" / "or"
public class NotExpr : Expr { public Expr Inner; }
public class AskExpr : Expr { public Expr Prompt; }                            // Prompt can be null
public class RandomExpr : Expr { public Expr Low, High; }
public class CallExpr : Expr { public string Name; public List<Expr> Args = new(); }
public class ListExpr : Expr { public List<Expr> Items = new(); }               // list 1 2 3
public class BoxExpr : Expr { public List<(string Name, Expr Value)> Parts = new(); } // box health 100 name "grok"
public class PartExpr : Expr { public string Part; public Expr Holder; }       // health of player
public class ItemExpr : Expr { public Expr Index; public Expr Holder; }        // item 2 of bag
public class SizeExpr : Expr { public Expr Inner; }                            // size of bag
public class KindExpr : Expr { public Expr Inner; }                            // kind of x

// ---------- lines (statements) ----------

public abstract class Stmt : Node { }

public class KindStmt : Stmt { public string Kind; }                           // me is player
public class HasStmt : Stmt { public string Name; public Expr Value; }          // me has health 100
public class SetStmt : Stmt { public Expr Target; public Expr Value; }          // health is 50 / health of p is 50
public class ChangeStmt : Stmt { public Expr Target; public bool Gain; public Expr Amount; } // health gain/lose 5
public class SayStmt : Stmt { public Expr Value; }
public class IfStmt : Stmt
{
    public List<(Expr Cond, List<Stmt> Body)> Branches = new();
    public List<Stmt> Else;                                                    // null when no else
}
public class RepeatStmt : Stmt { public Expr Times; public List<Stmt> Body; }
public class RepeatWhileStmt : Stmt { public Expr Cond; public List<Stmt> Body; }
public class CountStmt : Stmt { public string Name; public Expr From, To; public List<Stmt> Body; }
public class EachStmt : Stmt { public string Name; public Expr Source; public List<Stmt> Body; } // each x in bag
public class StopStmt : Stmt { }
public class SkipStmt : Stmt { }
public class GiveStmt : Stmt { public Expr Value; }                             // Value can be null
public class WaitStmt : Stmt { public Expr Seconds; }
public class DieStmt : Stmt { }
public class CallStmt : Stmt { public CallExpr Call; }
public class CanStmt : Stmt { public string Name; public List<string> Params = new(); public List<Stmt> Body; } // me can jump
public class TryStmt : Stmt { public List<Stmt> Body; public string OopsName; public List<Stmt> Oops; } // try / oops why
public class FailStmt : Stmt { public Expr Message; }                           // fail "too big"
public class UseStmt : Stmt { public string Path; }                             // use "tools.ooga"

public class OogaProgram : Node { public List<Stmt> Body = new(); }
