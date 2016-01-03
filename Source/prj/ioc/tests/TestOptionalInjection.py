
import sys
import os
from random import randint
import unittest

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject, InjectOptional
import prj.ioc.IocAssertions as Assertions

class TestConditionalInjections(unittest.TestCase):
    ''' Test assertions in inject statements '''
    def setUp(self):
        Container.clear()

    def testUseDefault(self):
        Container.bind('Foo').to(1)
        Container.bind('Test1').toSingle(Test1)

        test1 = Container.resolve('Test1')

        self.assertEqual(test1.foo, 1)
        self.assertEqual(test1.bar, 5)

    def testOverrideDefault(self):
        Container.bind('Foo').to(2)
        Container.bind('Test1').to(Test1)
        Container.bind('Bar').to(3)

        test1 = Container.resolve('Test1')

        self.assertEqual(test1.foo, 2)
        self.assertEqual(test1.bar, 3)

# Helper methods
class Test1:
    foo = Inject('Foo')
    bar = InjectOptional('Bar', 5)

if __name__ == '__main__':
    unittest.main()

