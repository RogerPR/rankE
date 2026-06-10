import constants as c
import abilities_impl as a
import random
from ui_aux import trigger_screen_flash


class Character:
    def __init__(self, name, max_hp):
        self.name = name
        self.hp = max_hp
        self.global_cooldown = 0
        self.global_cooldown_constant = c.GLOBAL_COOLDOWN_TICKS
        self.armor = 1
        self.stance = "rock"
        self.stance_cooldown = 0
        self.quick_action_cooldown = 0
        self.auto_attack_cooldown = 0
        self.auto_attack_interval = c.AUTO_ATTACK_INTERVAL
        self.conditions = {}  # {condition_name: duration}
        self.spell_gems = (
            c.SPELL_GEMS if self.name == "Player" else c.SPELL_GEMS_OPPONENT
        )
        self.extra_cooldown_mult = 1
        self.n_decisions = 0

        self.selected_stance = "rock"
        self.selected_weapon = "sword"
        self.selected_armor = "medium"

        self.casting = ""
        self.cast_ticks_remaining = 0
        self.time_casting = 0
        self.next_action = "do_nothing"

        self.riposte = False
        self.parry_counter = 0  # Track parry progress (0-8)
        self.parry_counter_max = 8  # Maximum parry counter value

        self.abilities = {
            "slash": a.Slash(),
            "bash": a.Bash(),
            "fireball": a.Fireball(),
            "vampiro": a.Vampiro(),
            "parry": a.Parry(),
            "riposte": a.Riposte(),
            "kick": a.Kick(),
            "interrupt_cast": a.InterruptCast(),
            "change_stance_rock": a.ChangeStanceRock(),
            "change_stance_wind": a.ChangeStanceWind(),
            "change_stance_water": a.ChangeStanceWater(),
            "quick_action": a.Kick(),  # ADD for CD display in UI
            "auto_attack": a.AutoAttack(),
        }

    def update_stance(self, stance):
        self.stance = stance

        # All stance switch cooldowns are shared
        self.abilities["change_stance_rock"].remaining_cooldown = (
            c.STANCE_SWITCH_COOLDOWN
        )
        self.abilities["change_stance_wind"].remaining_cooldown = (
            c.STANCE_SWITCH_COOLDOWN
        )
        self.abilities["change_stance_water"].remaining_cooldown = (
            c.STANCE_SWITCH_COOLDOWN
        )

    def update_choices(self):

        self.update_choice_stance()
        self.update_choice_weapon()
        self.update_choice_armor()

    def update_choice_stance(self):

        if self.selected_stance == "rock":

            self.abilities["auto_attack"].damage = 8

        elif self.selected_stance == "wind":
            self.hp = int(self.hp * 0.9)
            self.abilities["parry"].cooldown = int(self.abilities["parry"].cooldown / 2)

        elif self.selected_stance == "water":
            self.hp = int(self.hp * 0.8)
            self.spell_gems = 7

    def update_choice_weapon(self):
        self.selected_weapon = self.selected_weapon

        if self.selected_weapon == "sword":
            self.extra_cooldown_mult = 1

        elif self.selected_weapon == "dagger":
            self.auto_attack_interval = int(self.auto_attack_interval * 0.8)
            self.abilities["auto_attack"].damage = int(
                self.abilities["auto_attack"].damage * 0.9
            )

        elif self.selected_weapon == "wand":
            self.abilities["fireball"].cast_time = int(
                self.abilities["fireball"].cast_time * 0.8
            )
            self.abilities["vampiro"].cast_time = int(
                self.abilities["vampiro"].cast_time * 0.8
            )

    def update_choice_armor(self):

        if self.selected_armor == "light":
            self.armor = 0.9
            self.extra_cooldown_mult = 0.9

        elif self.selected_armor == "medium":
            self.armor = 1
            self.extra_cooldown_mult = 1
        elif self.selected_armor == "heavy":
            self.armor = 1.1
            self.extra_cooldown_mult = 1.1

    def autoattack(self):

        if self.can_act() and not self.casting and self.auto_attack_cooldown <= 0:
            auto_attack_result = self.abilities["auto_attack"].use()
            self.auto_attack_cooldown = self.auto_attack_interval
            return auto_attack_result
        else:
            return {}

    def tick(self):
        if self.global_cooldown > 0:
            self.global_cooldown -= 1

        if self.casting:
            self.cast_ticks_remaining -= 1
            self.time_casting += 1

        if self.auto_attack_cooldown > 0:
            self.auto_attack_cooldown -= 1

        for ability in self.abilities.values():
            ability.tick()

        for k, v in list(self.conditions.items()):

            if k == "poison":
                if v[0] % c.APPLICATION_INTERVAL == 0:
                    self.hp -= c.POISON_DOT

            if k == "regen":
                if v[0] % c.APPLICATION_INTERVAL == 0:
                    self.hp += c.REGEN_HEAL

            if k == "stun":
                self.casting = ""
                self.cast_ticks_remaining = 0

            v[0] -= 1
            if v[0] <= 0:
                del self.conditions[k]

    def can_act(self):
        if "stun" in self.conditions:
            return False
        else:
            return True

    def start_casting(self, name):
        self.casting = name
        self.cast_ticks_remaining = self.abilities[name].cast_time

    def act(self, ability_name):
        # If Just finished casting -> Cast
        if (self.casting != "") and (self.cast_ticks_remaining <= 0):
            # Check if we have enough spell gems for spell abilities
            # if self.casting in ["fireball", "vampiro"] and self.spell_gems <= 0:
            #     self.casting = ""
            #     self.time_casting = 0
            #     return {}

            ability_result = self.abilities[self.casting].use(self.extra_cooldown_mult)
            # Consume a spell gem for spell abilities
            if self.casting in ["fireball", "vampiro"]:
                self.spell_gems -= 1
            self.casting = ""
            self.time_casting = 0
            return ability_result

        if ability_name == "quick_action":
            ability_name = c.STANCE_ACTION[self.stance]

        if ability_name == "do_nothing":
            return {}

        if not self.can_act():
            return {}

        if self.riposte:
            ability_name = "riposte"
            self.riposte = False

        # if (
        #     (self.cast_ticks_remaining > 0)
        #     and ability_name in ["fireball", "vampiro"]
        #     and self.time_casting > 3
        # ):
        #     ability_name = "interrupt_cast"
        #     self.time_casting = 0

        # Cant act while casting unless specifically allowed
        if (self.cast_ticks_remaining > 0) and ability_name not in c.ACTIONS_WHILE_CAST:
            return {}

        # Check if we have enough spell gems for spell abilities
        if ability_name in ["fireball", "vampiro"] and self.spell_gems <= 0:
            return {}

        # Quick action abilities
        elif (ability_name in c.QUICK_ACTIONS) and (
            self.abilities[ability_name].is_ready()
        ):
            ability_result = self.abilities[ability_name].use(self.extra_cooldown_mult)
            # Quick actions cause global cooldown -> LESS
            self.global_cooldown = c.GLOBAL_COOLDOWN_TICKS_QUICK_ACTION

        # Normal abilities
        elif (self.global_cooldown == 0) and (self.abilities[ability_name].is_ready()):
            # Is instant ability
            if self.abilities[ability_name].cast_time == 0:
                ability_result = self.abilities[ability_name].use(
                    self.extra_cooldown_mult
                )
                # Consume a spell gem for instant spell abilities
                if ability_name in ["fireball", "vampiro"]:
                    self.spell_gems -= 1
            else:
                self.start_casting(ability_name)
                ability_result = {}

            self.global_cooldown = self.global_cooldown_constant
        else:
            ability_result = {}

        return ability_result

    def receive_action(self, action, self_action=False):
        # Select actions that apply to self or opponent
        if self_action:
            act = {k.replace("self_", ""): v for k, v in action.items() if "self_" in k}
        else:
            act = {k: v for k, v in action.items() if "self_" not in k}

        # Parry action, riposte and avoid effects
        if ("parriable" in act) and ("parry" in self.conditions):
            # Increment parry counter by 2
            self.parry_counter = min(self.parry_counter + 2, self.parry_counter_max)
            # Trigger screen flash when parry counter is incremented
            trigger_screen_flash()
            # Only trigger riposte when counter is full
            if self.parry_counter >= self.parry_counter_max:
                self.riposte = True
                self.parry_counter = 0  # Reset counter after riposte
            return None

        # Receive action
        for k, v in act.items():
            if k == "damage":
                self.hp -= int(self.armor * v)

            if k == "condition":
                # Counter + original duration
                self.conditions[v] = [
                    act["condition_duration"],
                    act["condition_duration"],
                ]

            if k == "stance":
                self.update_stance(v)

            if k == "action":
                self.modify_self_action(v)

    def modify_self_action(self, action):

        if action == "stop_casting":
            self.casting = ""
            self.cast_ticks_remaining = 0

        if action == "kick":
            if self.cast_ticks_remaining > 0:
                if self.abilities[self.casting].interruptible:
                    self.casting = ""
                    self.cast_ticks_remaining = 0
                    self.conditions[c.KICK_CONDITION] = [
                        c.KICK_DURATION,
                        c.KICK_DURATION,
                    ]

    def decide_what_to_do(self, opponent):
        # return "do_nothing"
        # If stunned or casting and can't act, do nothing
        if not self.can_act():
            act = "do_nothing"

        elif self.n_decisions > 50 * 2:  # 5 seconds
            self.n_decisions = 0
            act = "slash"

        elif self.abilities["parry"].is_ready():
            act = "parry"

        # Interrupt opponent if they're casting something interruptible
        elif (opponent.casting != "") and self.abilities["kick"].is_ready():
            if random.random() < (0.2 / (opponent.cast_ticks_remaining + 1)):
                act = "kick"
            else:
                act = "do_nothing"

        # If HP is low, try to heal with Vampiro if it's ready
        elif (
            self.hp < c.MAX_HP * 0.6
            and self.abilities["vampiro"].is_ready()
            and self.spell_gems > 0
        ):
            act = "vampiro"

        # Try to use Fireball if not on cooldown and we're not casting
        elif (
            self.abilities["fireball"].is_ready()
            and self.cast_ticks_remaining == 0
            and self.spell_gems > 1
        ):
            act = "fireball"

        # Default to slash if available
        elif self.abilities["bash"].is_ready():
            act = "bash"

        # Otherwise, do nothing
        else:
            act = "do_nothing"

        self.next_action = act
        self.n_decisions += 1
        return act
