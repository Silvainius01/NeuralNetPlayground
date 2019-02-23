using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public delegate int IntDelegate0();
public delegate bool BoolDelegate0();
public delegate float FloatDelegate0();
public delegate double DoubleDelegate0();
public delegate void VoidDelegate0();

public delegate int IntDelegate1<T>(T p0);
public delegate bool BoolDelegate1<T>(T p0);
public delegate float FloatDelegate1<T>(T p0);
public delegate double DoubleDelegate1<T>(T p0);
public delegate void VoidDelegate1<T>(T p0);

public delegate int IntDelegate2<T0, T1>(T0 p0, T1 p1);
public delegate bool BoolDelegate2<T0, T1>(T0 p0, T1 p1);
public delegate float FloatDelegate2<T0, T1>(T0 p0, T1 p1);
public delegate double DoubleDelegate2<T0, T1>(T0 p0, T1 p1);
public delegate void VoidDelegate2<T0, T1>(T0 p0, T1 p1);