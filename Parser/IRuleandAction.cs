///////////////////////////////////////////////////////////////////////////
// IRuleAndAction.cs - Interfaces & abstract bases for rules and actions //
// ver 1.1                                                               //
// Language:    C#, 2008, .Net Framework 4.0                             //
// Platform:    Dell Precision T7400, Win7, SP1                          //
// Application: Demonstration for CSE681, Project #2, Fall 2011          //
// Author:      Jim Fawcett, CST 4-187, Syracuse University              //
//              (315) 443-3948, jfawcett@twcny.rr.com                    //
///////////////////////////////////////////////////////////////////////////
/*
 * Module Operations:
 * ------------------
 * This module defines the following classes:
 *   IRule   - interface contract for Rules
 *   ARule   - abstract base class for Rules that defines some common ops
 *   IAction - interface contract for rule actions
 *   AAction - abstract base class for actions that defines common ops
 */
/* Required Files:
 *   IRuleAndAction.cs
 *   
 * Build command:
 *   Interfaces and abstract base classes only so no build
 *   
 * Maintenance History:
 * --------------------
 * ver 1.1 : 11 Sep 2011
 * - added properties displaySemi and displayStack
 * ver 1.0 : 28 Aug 2011
 * - first release
 *
 * Note:
 * This package does not have a test stub as it contains only interfaces
 * and abstract classes.
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;

namespace PackageAnalysis
{
  /////////////////////////////////////////////////////////
  // contract for actions used by parser rules

  public interface IAction
  {
    void doAction(CSsemi.CSemiExp semi);
  }
  /////////////////////////////////////////////////////////
  // abstract action base supplying common functions

  public abstract class AAction : IAction
  {
    static bool displaySemi_ = false;   // default
    static bool displayStack_ = false;  // default

    public abstract void doAction(CSsemi.CSemiExp semi);

    public static bool displaySemi 
    {
      get { return displaySemi_; }
      set { displaySemi_ = value; }
    }
    public static bool displayStack 
    {
      get { return displayStack_; }
      set { displayStack_ = value; }
    }

    public virtual void display(CSsemi.CSemiExp semi)
    {
      if(displaySemi)
        for (int i = 0; i < semi.count; ++i)
          Console.Write("{0} ", semi[i]);
    }
  }
  /////////////////////////////////////////////////////////
  // contract for parser rules

  public interface IRule
  {
    bool test(CSsemi.CSemiExp semi);
    void add(IAction action);
  }
  /////////////////////////////////////////////////////////
  // abstract rule base implementing common functions

  public abstract class ARule : IRule
  {
    private List<IAction> actions;
    public ARule()
    {
      actions = new List<IAction>();
    }
    public void add(IAction action)
    {
      actions.Add(action);
    }
    abstract public bool test(CSsemi.CSemiExp semi);
    public void doActions(CSsemi.CSemiExp semi)
    {
      foreach (IAction action in actions)
        action.doAction(semi);
    }
    public int indexOfType(CSsemi.CSemiExp semi)
    {
      int indexCL = semi.Contains("class");
      int indexIF = semi.Contains("interface");
      int indexST = semi.Contains("struct");
      int indexEN = semi.Contains("enum");
      //int indexDE = semi.Contains("delegate");

      int index = Math.Max(indexCL, indexIF);
      index = Math.Max(index, indexST);
      index = Math.Max(index, indexEN);
      //index = Math.Max(index, indexDE);
      return index;
    }
  }
}

