from abc import ABC, abstractmethod


class Ability(ABC):
    def __init__(self, name, cooldown, cast_time=0, interruptible=True):
        self.name = name
        self.cooldown = cooldown
        self.cast_time = cast_time
        self.interruptible = interruptible

        self.remaining_cooldown = 0

    def is_ready(self):
        return self.remaining_cooldown == 0

    def use(self, extra_cooldown_mult=1):
        """Triggers the ability: starts cooldown and potentially casting."""
        self.remaining_cooldown = int(self.cooldown * extra_cooldown_mult)
        return self.activate()  # Immediate activation

    def tick(self):
        """Updates cooldown and casting progress."""
        if self.remaining_cooldown > 0:
            self.remaining_cooldown -= 1

    @abstractmethod
    def activate(self):
        """To be implemented by subclasses: what the ability actually *does* when triggered."""
        pass
