
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

class TestCircularDependencies(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        Container.bind('Foo1').to(Foo1, 7)
        Container.bind('Foo2').to(Foo2, 6)

        foo2 = Container.resolve('Foo2')
        foo2.Start()

class Foo1:
    foo2 = Inject('Foo2')

    def __init__(self, val):
        self.val = val

    def Start(self):
        print(self.foo2.val)

class Foo2:
    foo1 = Inject('Foo1')

    def __init__(self, val):
        self.val = val

    def Start(self):
        print('foo2 {0}, foo1 {1}'.format(self.val, self.foo1.val))

if __name__ == '__main__':
    unittest.main()

