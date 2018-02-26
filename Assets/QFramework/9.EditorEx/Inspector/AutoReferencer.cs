﻿/****************************************************************************
 * Copyright (c) 2017 Karsion
****************************************************************************/
// Date: 2018-02-01
// Time: 16:37
// Author: Karsion

using System;
using System.Collections.Generic;
using System.Reflection;
using QFramework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AutoReferencer
{
    public static void CalledByEditor(IEnumerable<Object> targets)
    {
        foreach (Object item in targets)
        {
            Transform tf = item as Transform;
            MonoBehaviour[] monos = tf.GetComponents<MonoBehaviour>();
            Undo.RecordObjects(monos, "CalledByEditor");
            for (int i = 0; i < monos.Length; i++)
            {
                //找脚本上的FR函数，编辑器调用
                MethodInfo methodInfo = monos[i].GetType()
                                                .GetMethod("CalledByEditor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo == null)
                {
                    continue;
                }
                Debug.Log(monos[i].name + " Method CalledByEditor Invoke");
                methodInfo.Invoke(monos[i], null);
            }
        }
    }

    public static void FindReferences(IEnumerable<Object> targets)
    {
        foreach (Object item in targets)
        {
            Transform tf = item as Transform;
            MonoBehaviour[] monos = tf.GetComponents<MonoBehaviour>();
            Undo.RecordObjects(monos, "FindReferences");
            for (int i = 0; i < monos.Length; i++)
            {
                //按照变量名自动找引用
                FindReferences(monos[i]);
            }
        }
    }

    private static void FindReferences(MonoBehaviour mono)
    {
        foreach (FieldInfo field in mono.GetType().GetFields()) //遍历T类的变量
        {
            object objValue = field.GetValue(mono);
            Type fieldType = field.FieldType;

            #region 自动找数组功能

            //如果是数组
            if (fieldType.IsArray)
            {
                //判断是不是空的数组或者数组元素有填充了，就跳过
                object[] objs = objValue as object[];
                if (objs == null || objs.Length > 0)
                {
                    continue;
                }

                //处理GameObject数组
                Array filledArray;
                Type elementType = fieldType.GetElementType();
                if (elementType == typeof(GameObject))
                {
                    Transform[] tfs = mono.GetComponentsInChildren<Transform>();
                    Transform[] tfHits = Array.FindAll(tfs, item => item.name.StartsWith(field.Name));
                    int nLength = tfHits.Length;
                    GameObject[] gos = new GameObject[nLength];
                    for (int i = 0; i < nLength; i++)
                    {
                        gos[i] = tfHits[i].gameObject;
                    }

                    filledArray = Array.CreateInstance(elementType, nLength);
                    Array.Copy(gos, filledArray, nLength);
                    field.SetValue(mono, filledArray);
                    continue;
                }

                Component[] coms = mono.GetComponentsInChildren(elementType);
                Component[] comHits = Array.FindAll(coms, item => item.name.StartsWith(field.Name));
                if (elementType != null)
                {
                    filledArray = Array.CreateInstance(elementType, comHits.Length);
                    Array.Copy(comHits, filledArray, comHits.Length);
                    field.SetValue(mono, filledArray);
                }

                continue;
            }
            #endregion

            //如果不空，就跳过。但是GameObject类型和Transform类型的object值是字符串"null"，很恶心，要转字符串比较
            if (objValue != null && fieldType != typeof(string) && !objValue.ToString().Equals("null"))
            {
                continue;
            }

            //查找自身的变量
            if (field.Name.Contains("Self"))
            {
                if (fieldType == typeof(GameObject))
                {
                    field.SetValue(mono, mono.gameObject);
                    continue;
                }

                Component com = mono.GetComponent(fieldType);
                if (com)
                {
                    field.SetValue(mono, com);
                    continue;
                }
            }

            //迭代遍历子物体看看有没有同名的
            FieldInfo info = field;
            Transform tf = mono.transform.FindChildRecursion(tfChild => tfChild.name.Equals(info.Name));
            if (tf == null)
            {
                continue;
            }

            //赋值操作
            if (fieldType == typeof(GameObject))
            {
                field.SetValue(mono, tf.gameObject);
            }
            else
            {
                field.SetValue(mono, tf.GetComponent(fieldType));
            }
        }
    }
}