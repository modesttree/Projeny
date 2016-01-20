import collections
from mtm.util.Assert import *

_providers = {}
_singletons = {}

def bind(identifier):
    return Binder(identifier)

def hasBinding(identifier):
    return identifier in _providers

def resolve(identifier):
    try:
        assertThat(identifier in _providers, \
            'Could not find dependency with identifier "{0}"'.format(identifier))
        matches = _providers[identifier]

        assertThat(len(matches) == 1, \
            'Found multiple matches when only one was expected for identifier "{0}"'.format(identifier))

        provider = matches[0]
    except KeyError:
        raise KeyError("Unknown identifier named %r" % identifier)

    return provider()

def resolveMany(identifier):
    if not identifier in _providers:
        return []

    return [p() for p in _providers[identifier]]

def clear():
    _providers.clear()
    _singletons.clear()

class Binder:
    def __init__(self, identifier):
        self.identifier = identifier

    def to(self, provider, *args, **kwargs):
        if isinstance(provider, collections.Callable):
            def call():
                return provider(*args, **kwargs)
        else:
            def call():
                return provider

        self._toProvider(call)

    def toSingle(self, provider, *args, **kwargs):
        '''
        Provider can be a class or a method
        In both cases you can also provide arguments using the rest
        of the arguments
        '''
        assertThat(not type(provider) in (str, int, float))

        if isinstance(provider, collections.Callable):
            # It is either a method or a class
            def call():
                instance = _singletons.get(provider)
                if not instance:
                    instance = provider(*args, **kwargs)
                    _singletons[provider] = instance
                return instance
        else:
            # Assume its an instance
            assertThat(not type(provider) in _singletons)
            _singletons[type(provider)] = provider
            def call():
                return provider

        self._toProvider(call)

    def _toProvider(self, provider):
        if not self.identifier in _providers:
            _providers[self.identifier] = []

        _providers[self.identifier].append(provider)

