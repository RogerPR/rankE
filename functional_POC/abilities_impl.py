from ability import Ability
from constants import *


class ChangeStanceRock(Ability):
    def __init__(self):
        super().__init__("change_stance_rock", cooldown=STANCE_SWITCH_COOLDOWN)

    def activate(self):
        return {"self_stance": "rock"}


class ChangeStanceWind(Ability):
    def __init__(self):
        super().__init__("change_stance_wind", cooldown=STANCE_SWITCH_COOLDOWN)

    def activate(self):
        return {"self_stance": "wind"}


class ChangeStanceWater(Ability):
    def __init__(self):
        super().__init__("change_stance_water", cooldown=STANCE_SWITCH_COOLDOWN)

    def activate(self):
        return {"self_stance": "water"}


class Kick(Ability):
    def __init__(self):
        super().__init__("kick", cooldown=QUICK_ACTION_COOLDOWN)

    def activate(self):
        return {"damage": KICK_DAMAGE, "action": KICK_ACTION}


class Parry(Ability):
    def __init__(self):
        super().__init__("parry", cooldown=QUICK_ACTION_COOLDOWN)

    def activate(self):
        return {
            "self_condition": PARRY_CONDITION,
            "self_condition_duration": PARRY_DURATION,
        }


class Riposte(Ability):
    def __init__(self):
        super().__init__("riposte", cooldown=0)

    def activate(self):
        return {
            "condition": RIPOSTE_CONDITION,
            "condition_duration": RIPOSTE_DURATION,
            "damage": RIPOSTE_DAMAGE,
        }


class InterruptCast(Ability):
    def __init__(self):
        super().__init__("interrupt_cast", cooldown=0)

    def activate(self):
        return {"self_action": INTERRUP_CAST_ACTION}


class Slash(Ability):
    def __init__(self):
        super().__init__("slash", cooldown=SLASH_COOLDOWN)

    def activate(self):
        return {"damage": SLASH_DAMAGE, "parriable": True}


class Bash(Ability):
    def __init__(self):
        super().__init__("bash", cooldown=STUN_COOLDOWN)
        self.parriable = True

    def activate(self):
        return {
            "damage": STUN_DAMAGE,
            "condition": STUN_CONDITION,
            "condition_duration": STUN_DURATION,
            "parriable": True,
        }


class Fireball(Ability):
    def __init__(self):
        super().__init__(
            "fireball", cooldown=FIREBALL_COOLDOWN, cast_time=FIREBALL_CAST_TIME
        )

    def activate(self):
        return {
            "damage": FIREBALL_DAMAGE,
        }


class Vampiro(Ability):
    def __init__(self):
        super().__init__(
            "vampiro", cooldown=VAMPIRO_COOLDOWN, cast_time=VAMPIRO_CAST_TIME
        )

    def activate(self):
        return {
            "self_condition": VAMPIRO_SELF_CONDITION,
            "self_condition_duration": VAMPIRO_SELF_DURATION,
            "condition": VAMPIRO_CONDITION,
            "condition_duration": VAMPIRO_DURATION,
        }


class AutoAttack(Ability):
    def __init__(self):
        super().__init__(
            "auto_attack",
            cooldown=0,
            cast_time=0,
        )
        self.damage = AUTO_ATTACK_DAMAGE

    def activate(self):
        return {
            "damage": self.damage,
            "parriable": True,
        }

        # # Handle autoattack
        # if self.auto_attack_cooldown > 0:
        #     self.auto_attack_cooldown -= 1
        # elif (
        #     self.can_act() and not self.casting
        # ):  # Only autoattack if not stunned or casting
        #     auto_attack_result = self.abilities["auto_attack"].use()
        #     self.auto_attack_cooldown = c.AUTO_ATTACK_INTERVAL
        #     return auto_attack_result
