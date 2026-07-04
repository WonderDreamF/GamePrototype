namespace GamePrototype.Core {
  public delegate void EventDelegate();

  public delegate void EventDelegate<T>(T arg);
}
