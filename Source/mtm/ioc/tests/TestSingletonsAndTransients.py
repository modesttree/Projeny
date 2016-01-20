
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

class TestSingletonsAndTransients(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def testSingletonType(self):
        Container.bind('Qux').toSingle(Qux)

        test1 = Test1()
        test2 = Test2()

        self.assertIs(test1.qux, test2.qux)

    def testSingletonTypeWithParam(self):
        Container.bind('Qux').toSingle(Qux, 4)

        test1 = Test1()
        test2 = Test2()

        self.assertIs(test1.qux, test2.qux)
        self.assertEqual(test1.qux.GetValue(), 4)

    def testSingletonMethodWithParam(self):
        Container.bind('Qux').toSingle(GetQux, 4)

        test1 = Test1()
        test2 = Test2()

        self.assertIs(test1.qux, test2.qux)
        self.assertEqual(test1.qux.GetValue(), 4)

    def testTransientInstanceWithParam(self):
        Container.bind('Qux').to(Qux, 5)

        test1 = Test1()
        test2 = Test2()

        self.assertIsNot(test1.qux, test2.qux)
        self.assertEqual(test1.qux.GetValue(), 5)

    def testTransientInstance(self):
        Container.bind('Qux').to(Qux)

        test1 = Test1()
        test2 = Test2()

        self.assertIsNot(test1.qux, test2.qux)

    def testTransientMethod(self):

        Container.bind('Qux').to(GetQux, 6)

        test1 = Test1()
        test2 = Test2()

        self.assertIsNot(test1.qux, test2.qux)
        self.assertEqual(test1.qux.GetValue(), 6)

class Test1:
    qux = Inject('Qux')

    def __init__(self):
        self.X = 0

    def Run(self):
        print(self.qux.GetValue())

class Test2:
    qux = Inject('Qux')

    def __init__(self):
        self.X = 0

    def Run(self):
        print(self.qux.GetValue())

class Qux:
    def __init__(self, val = None):
        self.val = val

    def GetValue(self):
        return self.val

def GetQux(val):
    return Qux(val)

if __name__ == '__main__':
    unittest.main()
